﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OctoAwesome.Logging;
using OctoAwesome.Notifications;
using OctoAwesome.Serialization;
using OctoAwesome.Threading;

namespace OctoAwesome.Runtime
{
    /// <summary>
    /// Manager für die Weltelemente im Spiel.
    /// </summary>
    public class ResourceManager : IResourceManager
    {
        public Player CurrentPlayer
        {
            get => _player ?? (_player = LoadPlayer(""));
            private set => _player = value;
        }

        public IUpdateHub UpdateHub { get; private set; }

        private readonly bool _disablePersistence = false;
        private readonly IPersistenceManager _persistenceManager = null;
        private readonly ILogger _logger;
        private readonly List<IMapPopulator> _populators = null;
        private Player _player;
        private readonly LockSemaphore _lockSemaphoreSlim;

        /// <summary>
        /// Das aktuell geladene Universum.
        /// </summary>
        public IUniverse CurrentUniverse { get; private set; }

        /// <summary>
        /// Manager for all Definitions (Blocks, Entities, Items)
        /// </summary>
        public IDefinitionManager DefinitionManager { get; private set; }

        public ConcurrentDictionary<int, IPlanet> Planets { get; }

        private readonly IExtensionResolver _extensionResolver;

        private readonly CountedScopeSemaphore _loadingSemaphore;
        private CancellationToken _currentToken;
        private CancellationTokenSource _tokenSource;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="extensionResolver">ExetnsionResolver</param>
        /// <param name="definitionManager">DefinitionManager</param>
        /// <param name="settings">Einstellungen</param>
        public ResourceManager(IExtensionResolver extensionResolver, IDefinitionManager definitionManager, ISettings settings, IPersistenceManager persistenceManager)
        {
            _lockSemaphoreSlim = new LockSemaphore(1, 1);
            _loadingSemaphore = new CountedScopeSemaphore(0);
            _extensionResolver = extensionResolver;
            DefinitionManager = definitionManager;
            _persistenceManager = persistenceManager;

            _logger = (TypeContainer.GetOrNull<ILogger>() ?? NullLogger.Default).As(typeof(ResourceManager));

            _populators = extensionResolver.GetMapPopulator().OrderBy(p => p.Order).ToList();

            Planets = new ConcurrentDictionary<int, IPlanet>();

            bool.TryParse(settings.Get<string>("DisablePersistence"), out _disablePersistence);
        }

        /// <summary>
        /// Insert UpdateHub for Entities
        /// </summary>
        /// <param name="updateHub"><see cref="UpdateHub"/></param>
        public void InsertUpdateHub(UpdateHub updateHub) => UpdateHub = updateHub;

        /// <summary>
        /// Erzuegt ein neues Universum.
        /// </summary>
        /// <param name="name">Name des neuen Universums.</param>
        /// <param name="seed">Weltgenerator-Seed für das neue Universum.</param>
        /// <returns>Die Guid des neuen Universums.</returns>
        public Guid NewUniverse(string name, int seed)
        {
            _loadingSemaphore.Wait();

            if(CurrentUniverse != null)
                UnloadUniverse();

            using (_loadingSemaphore.EnterScope())
            {
                _tokenSource?.Dispose();
                _tokenSource = new CancellationTokenSource();
                _currentToken = _tokenSource.Token;

                var guid = Guid.NewGuid();
                CurrentUniverse = new Universe(guid, name, seed);
                _persistenceManager.SaveUniverse(CurrentUniverse);
                return guid;
            }
        }

        /// <summary>
        /// Gibt alle Universen zurück, die geladen werden können.
        /// </summary>
        /// <returns>Die Liste der Universen.</returns>
        public IUniverse[] ListUniverses()
        {
            var awaiter = _persistenceManager.Load(out var universes);

            if (awaiter == null)
                return Array.Empty<IUniverse>();
            else
                awaiter.WaitOnAndRelease();

            return universes.ToArray();
        }

        /// <summary>
        /// Lädt das Universum mit der angegebenen Guid.
        /// </summary>
        /// <param name="universeId">Die Guid des Universums.</param>
        /// <returns>Das geladene Universum.</returns>
        public bool TryLoadUniverse(Guid universeId)
        {
            _loadingSemaphore.Wait();

            // Alte Daten entfernen
            if (CurrentUniverse != null)
                UnloadUniverse();

            // Neuen Daten loaden/generieren
            using (_loadingSemaphore.EnterScope())
            {
                _tokenSource?.Dispose();
                _tokenSource = new CancellationTokenSource();
                _currentToken = _tokenSource.Token;

                var awaiter = _persistenceManager.Load(out var universe, universeId);

                if (awaiter == null)
                    return false;
                else
                    awaiter.WaitOnAndRelease();

                CurrentUniverse = universe;

                if (CurrentUniverse == null)
                    throw new NullReferenceException();

                return true;
            }
        }

        /// <summary>
        /// Entlädt das aktuelle Universum.
        /// </summary>
        public void UnloadUniverse()
        {
            _loadingSemaphore.Wait();
            _tokenSource.Cancel();
            _loadingSemaphore.Wait();

            _persistenceManager.SaveUniverse(CurrentUniverse);

            foreach (var planet in Planets)
            {
                _persistenceManager.SavePlanet(CurrentUniverse.Id, planet.Value);
                planet.Value.Dispose();
            }

            Planets.Clear();

            CurrentUniverse = null;
            GC.Collect();
        }

        /// <summary>
        /// Gibt das aktuelle Universum zurück
        /// </summary>
        /// <returns>Das gewünschte Universum, falls es existiert</returns>
        public IUniverse GetUniverse() => CurrentUniverse;

        /// <summary>
        /// Löscht ein Universum.
        /// </summary>
        /// <param name="id">Die Guid des Universums.</param>
        public void DeleteUniverse(Guid id)
        {
            if (CurrentUniverse != null && CurrentUniverse.Id == id)
                throw new Exception("Universe is already loaded");

            _persistenceManager.DeleteUniverse(id);
        }

        /// <summary>
        /// Gibt den Planeten mit der angegebenen ID zurück
        /// </summary>
        /// <param name="id">Die Planteten-ID des gewünschten Planeten</param>
        /// <returns>Der gewünschte Planet, falls er existiert</returns>
        public IPlanet GetPlanet(int id)
        {
            if (CurrentUniverse == null)
                throw new Exception("No Universe loaded");

            _currentToken.ThrowIfCancellationRequested();
            
            using (_lockSemaphoreSlim.Wait())
            using (_loadingSemaphore.EnterScope())
            {
                if (!Planets.TryGetValue(id, out IPlanet planet))
                {
                    // Versuch vorhandenen Planeten zu laden
                    var awaiter = _persistenceManager.Load(out planet, CurrentUniverse.Id, id);

                    if (awaiter == null)
                    {
                        // Keiner da -> neu erzeugen
                        var rand = new Random(CurrentUniverse.Seed + id);
                        var generators = _extensionResolver.GetMapGenerator().ToArray();
                        var index = rand.Next(generators.Length - 1);
                        var generator = generators[index];

                        planet = generator.GeneratePlanet(CurrentUniverse.Id, id, CurrentUniverse.Seed + id);
                        // persistenceManager.SavePlanet(universe.Id, planet);
                    }
                    else
                    {
                        awaiter.WaitOnAndRelease();
                    }

                    Planets.TryAdd(id, planet);
                }
                return planet;
            }
        }

        /// <summary>
        /// Lädt einen Player.
        /// </summary>
        /// <param name="playerName">Der Name des Players.</param>
        /// <returns></returns>
        public Player LoadPlayer(string playerName)
        {
            if (CurrentUniverse == null)
                throw new Exception("No Universe loaded");

            _currentToken.ThrowIfCancellationRequested();

            using (_loadingSemaphore.EnterScope())
            {
                var awaiter = _persistenceManager.Load(out var player, CurrentUniverse.Id, playerName);

                if (awaiter == null)
                    player = new Player();
                else
                    awaiter.WaitOnAndRelease();

                return player;
            }
        }

        /// <summary>
        /// Speichert einen Player.
        /// </summary>
        /// <param name="player">Der Player.</param>
        public void SavePlayer(Player player)
        {
            if (CurrentUniverse == null)
                throw new Exception("No Universe loaded");

            using (_loadingSemaphore.EnterScope())
                _persistenceManager.SavePlayer(CurrentUniverse.Id, player);
        }

        public IChunkColumn LoadChunkColumn(IPlanet planet, Index2 index)
        {
            // Load from disk
            Awaiter awaiter;
            IChunkColumn column11;

            _currentToken.ThrowIfCancellationRequested();

            using (_loadingSemaphore.EnterScope())
            {
                do
                {
                    awaiter = _persistenceManager.Load(out column11, CurrentUniverse.Id, planet, index);

                    if (awaiter == null)
                    {
                        var column =
                            planet.Generator.GenerateColumn(DefinitionManager, planet, new Index2(index.X, index.Y));
                        column11 = column;
                        SaveChunkColumn(column);
                    }
                    else
                    {
                        awaiter.WaitOnAndRelease();
                    }

                    if (awaiter?.Timeouted ?? false)
                        _logger.Error("Awaiter timeout");

                } while (awaiter != null && awaiter.Timeouted);

                var column00 =
                    planet.GlobalChunkCache.Peek(Index2.NormalizeXY(index + new Index2(-1, -1), planet.Size));
                var column10 = planet.GlobalChunkCache.Peek(Index2.NormalizeXY(index + new Index2(0, -1), planet.Size));
                var column20 = planet.GlobalChunkCache.Peek(Index2.NormalizeXY(index + new Index2(1, -1), planet.Size));

                var column01 = planet.GlobalChunkCache.Peek(Index2.NormalizeXY(index + new Index2(-1, 0), planet.Size));
                var column21 = planet.GlobalChunkCache.Peek(Index2.NormalizeXY(index + new Index2(1, 0), planet.Size));

                var column02 = planet.GlobalChunkCache.Peek(Index2.NormalizeXY(index + new Index2(-1, 1), planet.Size));
                var column12 = planet.GlobalChunkCache.Peek(Index2.NormalizeXY(index + new Index2(0, 1), planet.Size));
                var column22 = planet.GlobalChunkCache.Peek(Index2.NormalizeXY(index + new Index2(1, 1), planet.Size));

                // Zentrum
                if (!column11.Populated && column21 != null && column12 != null && column22 != null)
                {
                    foreach (var populator in _populators)
                        populator.Populate(this, planet, column11, column21, column12, column22);

                    column11.Populated = true;
                }

                // Links oben
                if (column00 != null && !column00.Populated && column10 != null && column01 != null)
                {
                    foreach (var populator in _populators)
                        populator.Populate(this, planet, column00, column10, column01, column11);

                    column00.Populated = true;
                }

                // Oben
                if (column10 != null && !column10.Populated && column20 != null && column21 != null)
                {
                    foreach (var populator in _populators)
                        populator.Populate(this, planet, column10, column20, column11, column21);
                    column10.Populated = true;
                }

                // Links
                if (column01 != null && !column01.Populated && column02 != null && column12 != null)
                {
                    foreach (var populator in _populators)
                        populator.Populate(this, planet, column01, column11, column02, column12);
                    column01.Populated = true;
                }

                return column11;
            }
        }

        /// <summary>
        /// Serialize a Chunk
        /// </summary>
        /// <param name="chunkColumn"></param>
        public void SaveChunkColumn(IChunkColumn chunkColumn)
        {
            if (_disablePersistence)
                return;

            using (_loadingSemaphore.EnterScope())
                _persistenceManager.SaveColumn(CurrentUniverse.Id, chunkColumn.Planet, chunkColumn);
        }

        /// <summary>
        /// Deserialize an Entity
        /// </summary>
        /// <param name="entityId"><see cref="Entity"/></param>
        /// <returns></returns>
        public Entity LoadEntity(Guid entityId)
        {
            if (CurrentUniverse == null)
                throw new Exception("No Universe loaded");

            _currentToken.ThrowIfCancellationRequested();

            using (_loadingSemaphore.EnterScope())
            {
                var awaiter = _persistenceManager.Load(out var entity, CurrentUniverse.Id, entityId);

                if (awaiter == null)
                    return null;
                else
                    awaiter.WaitOnAndRelease();

                return entity;
            }
        }

        /// <summary>
        /// Serializes an Entity
        /// </summary>
        /// <param name="entity"><see cref="Entity"/></param>
        public void SaveEntity(Entity entity)
        {
            if (CurrentUniverse == null)
                throw new Exception("No Universe loaded");

            using (_loadingSemaphore.EnterScope())
            {
                if (entity is Player player)
                    SavePlayer(player);
                else
                    _persistenceManager.SaveEntity(entity, CurrentUniverse.Id);
            }
        }

        /// <summary>
        /// Loads the Entities with Components (BodyComponent, ..)
        /// </summary>
        /// <typeparam name="T"><see cref="Component"/></typeparam>
        /// <returns></returns>
        public IEnumerable<Entity> LoadEntitiesWithComponent<T>() where T : EntityComponent
        {
            _currentToken.ThrowIfCancellationRequested();

            using (_loadingSemaphore.EnterScope())
                return _persistenceManager.LoadEntitiesWithComponent<T>(CurrentUniverse.Id);
        }

        /// <summary>
        /// Get EntityIds with Components
        /// </summary>
        /// <typeparam name="T"><see cref="Component"/></typeparam>
        /// <returns></returns>
        public IEnumerable<Guid> GetEntityIdsFromComponent<T>() where T : EntityComponent
        {
            _currentToken.ThrowIfCancellationRequested();

            using (_loadingSemaphore.EnterScope())
                return _persistenceManager.GetEntityIdsFromComponent<T>(CurrentUniverse.Id);
        }

        /// <summary>
        /// Get IDs from all Entities
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Guid> GetEntityIds()
        {
            _currentToken.ThrowIfCancellationRequested();

            using (_loadingSemaphore.EnterScope())
                return _persistenceManager.GetEntityIds(CurrentUniverse.Id);
        }

        /// <summary>
        /// Get Components from Entities
        /// </summary>
        /// <typeparam name="T"><see cref="Component"/></typeparam>
        /// <param name="entityIds"><see cref="Entity"/></param>
        /// <returns></returns>
        public IEnumerable<(Guid Id, T Component)> GetEntityComponents<T>(IEnumerable<Guid> entityIds) where T : EntityComponent, new()
        {
            _currentToken.ThrowIfCancellationRequested();

            using (_loadingSemaphore.EnterScope())
                return _persistenceManager.GetEntityComponents<T>(CurrentUniverse.Id, entityIds);
        }
    }
}

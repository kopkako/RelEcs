using System;
using System.Collections.Generic;

namespace Bitron.Ecs
{
    public sealed class World
    {
        EntityMeta[] _entityMetas = new EntityMeta[512];
        int _entityCount = 1;

        int[] _despawnedEntities = new int[512];
        int _despawnedEntityCount = 0;

        int[] _storageIndices = new int[512];
        IStorage[] _storages = new IStorage[512];
        int _storageCount = 0;

        public Entity Spawn()
        {

            int id = 0;

            if (_despawnedEntityCount > 0)
            {
                id = _despawnedEntities[--_despawnedEntityCount];
            }
            else
            {
                id = _entityCount++;
            }

            ref var meta = ref _entityMetas[id];

            meta.Id = id;
            meta.Gen += 1;

            if (meta.BitSet == null)
            {
                meta.BitSet = new BitSet();
            }

            return new Entity() { Id = id, Gen = meta.Gen };
        }

        public void Despawn(Entity entity)
        {
            ref var meta = ref _entityMetas[entity.Id];

            if (meta.Id == 0)
            {
                return;
            }

            if (meta.BitSet.Count > 0)
            {
                for (int i = 1; i < _storageCount; i++)
                {
                    if (_storages[i].Has(entity))
                    {
                        _storages[i].Remove(entity);
                    }
                }
            }

            meta.Id = 0;
            meta.BitSet.ClearAll();

            if (_despawnedEntityCount == _despawnedEntities.Length)
            {
                Array.Resize(ref _despawnedEntities, _despawnedEntityCount << 1);
            }

            _despawnedEntities[_despawnedEntityCount++] = entity.Id;
        }

        public ref Component AddComponent<Component>(Entity entity) where Component : struct
        {
            var storage = GetStorage<Component>();

            ref var meta = ref _entityMetas[entity.Id];
            meta.BitSet.Set(storage.TypeId);

            return ref storage.Add(entity);
        }

        public void RemoveComponent<Component>(Entity entity) where Component : struct
        {
            var storage = GetStorage<Component>();

            ref var meta = ref _entityMetas[entity.Id];
            meta.BitSet.Clear(storage.TypeId);

            storage.Remove(entity);
        }

        public Entity[] Query(Mask mask)
        {
            List<Entity> entities = new List<Entity>();

            for (var i = 1; i < _entityCount; i++)
            {
                ref var meta = ref _entityMetas[i];

                if (mask.IsCompatibleWith(meta.BitSet))
                {
                    entities.Add(new Entity { Id = meta.Id, Gen = meta.Gen });
                }
            }

            return entities.ToArray();
        }

        public Storage<Component> GetStorage<Component>() where Component : struct
        {
            Storage<Component> storage = null;

            var typeId = ComponentType<Component>.Id;

            if (typeId >= _storageIndices.Length)
            {
                Array.Resize(ref _storageIndices, typeId << 1);
            }

            ref var storageId = ref _storageIndices[typeId];

            if (storageId > 0)
            {
                storage = _storages[storageId] as Storage<Component>;
            }
            else
            {
                storageId = ++_storageCount;
                Array.Resize(ref _storages, (_storageCount << 1));
                storage = new Storage<Component>(typeId);
                _storages[storageId] = storage;
            }

            return storage;
        }

        public bool IsAlive(Entity entity)
        {
            return _entityMetas[entity.Id].Id > 0;
        }
    }
}
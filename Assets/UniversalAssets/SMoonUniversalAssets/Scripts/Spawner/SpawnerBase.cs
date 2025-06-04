using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace SMoonUniversalAsset
{
    public abstract class SpawnerBase<T, G> where T : Component where G : Enum
    {
        [SerializeField] private Transform pool;
        [SerializeField] private List<SpawnProperty> spawnProperties;
        [SerializeField] private int initialPoolSize = 20;

        private List<SpawnProperty> spawnedPropertyPool;

        private int GetInitialPoolSize() => initialPoolSize * spawnProperties.Count;

        public virtual void Initialize()
        {
            int poolSize = GetInitialPoolSize();
            spawnedPropertyPool = new List<SpawnProperty>(poolSize);

            for (int i = 0; i < poolSize; i++)
            {
                foreach (var spawnProperty in spawnProperties)
                {
                    AddSpawnPropertyToSpawnedPropertyPool(spawnProperty, false, out _);
                }
            }
        }

        private void AddSpawnPropertyToSpawnedPropertyPool(List<SpawnProperty> spawnProperties, G type, bool setActiveValue, out T newComponent)
        {
            SpawnProperty selectedSpawnProperty = spawnProperties.FirstOrDefault(spawnProperty => spawnProperty.type.Equals(type));
            if (selectedSpawnProperty.component == null)
            {
                throw new IndexOutOfRangeException(type + " not found!");
            }
            AddSpawnPropertyToSpawnedPropertyPool(selectedSpawnProperty, setActiveValue, out newComponent);
        }

        private void AddSpawnPropertyToSpawnedPropertyPool(SpawnProperty spawnedProperty, bool setActiveValue, out T component)
        {
            component = UnityEngine.Object.Instantiate(spawnedProperty.component, pool);
            component.gameObject.SetActive(setActiveValue);
            spawnedProperty.component = component;
            spawnedPropertyPool.Add(spawnedProperty);
        }

        public T GetSpawned(G type, Vector3? position = null, Quaternion? rotation = null, Func<Vector3> onSetDeactiveOnDurationUpdate = null)
        {
            T component;
            foreach (var spawnedProperty in spawnedPropertyPool)
            {
                if (spawnedProperty.component.gameObject.activeInHierarchy)
                {
                    continue;
                }
                if (!spawnedProperty.type.Equals(type))
                {
                    continue;
                }

                component = spawnedProperty.component;
                if (position.HasValue)
                    component.transform.position = position.Value;

                if (rotation.HasValue)
                    component.transform.rotation = rotation.Value;

                component.gameObject.SetActive(true);
                OnSpawn(component, type, onSetDeactiveOnDurationUpdate);
                return component;
            }

            AddSpawnPropertyToSpawnedPropertyPool(spawnProperties, type, true, out component);
            OnSpawn(component, type, onSetDeactiveOnDurationUpdate);

            return component;
        }

        protected async void SetDeactiveOnDuration(T component, float duration, Func<Vector3> onSetDeactiveOnDurationUpdate = null)
        {
            if (onSetDeactiveOnDurationUpdate != null)
            {
                float time = 0;
                while (time < duration)
                {
                    Vector3 position = onSetDeactiveOnDurationUpdate.Invoke();
                    component.transform.position = position;
                    time += Time.deltaTime;
                    await UniTask.Yield();
                }
            }
            else
            {
                await UniTask.WaitForSeconds(duration);
            }
            component.gameObject.SetActive(false);
        }

        public int GetActiveSpawn()
        {
            int count = 0;
            foreach (var selector in spawnedPropertyPool)
            {
                if (selector.component.gameObject.activeInHierarchy)
                    count++;
            }
            return count;
        }

        public abstract void OnSpawn(T component, G type, Func<Vector3> onSetDeactiveOnDurationUpdate = null);


        [System.Serializable]
        public struct SpawnProperty
        {
            public G type;
            public T component;
        }
    }
}
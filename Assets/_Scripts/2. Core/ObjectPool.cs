using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Core
{
    public class FusionObjectPool : NetworkObjectProviderDefault
    {
        private Dictionary<NetworkPrefabId, Stack<NetworkObject>> _pool = new();

        protected override NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab)
        {
            if (_pool.TryGetValue(prefab.NetworkTypeId.AsPrefabId, out var stack) && stack.Count > 0)
            {
                NetworkObject instance = stack.Pop();
                instance.gameObject.SetActive(true);
                return instance;
            }

            //Using Instantiate instead of Spawn because Fusion calls this method as part of their runner.Spawn. We avoid an infinite recursive loop.
            NetworkObject newInstance = Instantiate(prefab);
            newInstance.gameObject.SetActive(true);
            return newInstance;
        }

        protected override void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
        {
            instance.gameObject.SetActive(false);

            instance.transform.SetParent(transform);
            instance.transform.localPosition = Vector3.zero;

            if (!_pool.TryGetValue(prefabId, out var stack))
            {
                stack = new Stack<NetworkObject>();
                _pool[prefabId] = stack;
            }

            stack.Push(instance);
        }

        public void ClearPools()
        {
            foreach (var stack in _pool.Values)
            {
                foreach (var instance in stack)
                {
                    if (instance != null) Destroy(instance.gameObject);
                }
            }
            _pool.Clear();
        }
    }
}
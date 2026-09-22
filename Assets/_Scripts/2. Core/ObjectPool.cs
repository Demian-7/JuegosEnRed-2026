using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Core
{
    public class FusionObjectPool : NetworkObjectProviderDefault
    {
        private Dictionary<NetworkObject, Stack<NetworkObject>> _pool = new();
        
        private Dictionary<NetworkObject, NetworkObject> _instanceToPrefabMap = new();

        protected override NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab)
        {
            if (_pool.TryGetValue(prefab, out var stack) && stack.Count > 0)
            {
                NetworkObject instance = stack.Pop();
                instance.gameObject.SetActive(true);
                return instance;
            }

            NetworkObject newInstance = Instantiate(prefab);
            newInstance.gameObject.SetActive(true);
            
            _instanceToPrefabMap[newInstance] = prefab;
            
            return newInstance;
        }

        protected override void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
        {
            if (_instanceToPrefabMap.TryGetValue(instance, out NetworkObject prefab))
            {
                instance.gameObject.SetActive(false);
                instance.transform.SetParent(transform);
                instance.transform.localPosition = Vector3.zero;

                if (!_pool.TryGetValue(prefab, out var stack))
                {
                    stack = new Stack<NetworkObject>();
                    _pool[prefab] = stack;
                }

                stack.Push(instance);
            }
            else
            {
                Destroy(instance.gameObject);
            }
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
            _instanceToPrefabMap.Clear();
        }
    }
}
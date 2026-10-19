using System;
using System.Collections.Concurrent;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Xprees.Core
{
    /// Utility extensions for resolving lifecycle and stateless status of ScriptableObjects.
    public static class StateLifetimeExtensions
    {
        // Lifetime from [StatefulLifetime] per type, null when the type has none. The miss is cached too: the reflection lookup is slow
        // (WebGL especially) and runs several times per asset on every scenario start.
        private readonly static ConcurrentDictionary<Type, StateLifetime?> _attributeLifetimeCache = new();
        private readonly static ConcurrentDictionary<Type, bool> _statelessCache = new();

        public static StateLifetime GetStateLifetime(this ScriptableObject so)
        {
            if (!so) return StateLifetime.Persistent;

            var attributeLifetime = _attributeLifetimeCache.GetOrAdd(so.GetType(),
                type => type.GetCustomAttribute<StatefulLifetimeAttribute>(true)?.Lifetime);
            if (attributeLifetime.HasValue) return attributeLifetime.Value;

            // Per asset, so it is not cached
            return so is DescriptionBaseSO descSo ? descSo.ConfiguredLifetime : StateLifetime.Persistent;
        }

        public static bool IsStateless(this ScriptableObject so)
        {
            if (!so) return true;

            var type = so.GetType();
            if (_statelessCache.TryGetValue(type, out var cached))
            {
                return cached;
            }

#if UNITY_EDITOR
            var statelessTypes = TypeCache.GetTypesWithAttribute<StatelessAssetAttribute>();
            var statelessTypesAlt = TypeCache.GetTypesWithAttribute<StatelessAttribute>();
            if (statelessTypes.Contains(type) || statelessTypesAlt.Contains(type))
            {
                _statelessCache[type] = true;
                return true;
            }
#endif
            var hasStatelessAttr = type.GetCustomAttribute<StatelessAssetAttribute>(true) != null ||
                                   type.GetCustomAttribute<StatelessAttribute>(true) != null;
            if (hasStatelessAttr)
            {
                _statelessCache[type] = true;
                return true;
            }

            // Assets are only stateful if they inherit from DescriptionBaseSO or are explicitly marked with StatefulLifetime
            var hasStatefulAttr = type.GetCustomAttribute<StatefulLifetimeAttribute>(true) != null;
            var isStateful = so is DescriptionBaseSO || hasStatefulAttr;

            var isStateless = !isStateful;
            _statelessCache[type] = isStateless;
            return isStateless;
        }
    }
}
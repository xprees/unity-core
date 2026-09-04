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
        private readonly static ConcurrentDictionary<Type, StateLifetime?> _lifetimeCache = new();
        private readonly static ConcurrentDictionary<Type, bool> _statelessCache = new();

        public static StateLifetime GetStateLifetime(this ScriptableObject so)
        {
            if (!so) return StateLifetime.Persistent;

            var type = so.GetType();
            if (_lifetimeCache.TryGetValue(type, out var cached) && cached.HasValue)
            {
                return cached.Value;
            }

#if UNITY_EDITOR
            var attrTypes = TypeCache.GetTypesWithAttribute<StatefulLifetimeAttribute>();
            if (attrTypes.Contains(type))
            {
                var attr = (StatefulLifetimeAttribute) Attribute.GetCustomAttribute(type, typeof(StatefulLifetimeAttribute), true);
                if (attr != null)
                {
                    _lifetimeCache[type] = attr.Lifetime;
                    return attr.Lifetime;
                }
            }
#endif
            var fallbackAttr = type.GetCustomAttribute<StatefulLifetimeAttribute>(true);
            if (fallbackAttr != null)
            {
                _lifetimeCache[type] = fallbackAttr.Lifetime;
                return fallbackAttr.Lifetime;
            }

            if (so is DescriptionBaseSO descSo)
            {
                return descSo.ConfiguredLifetime;
            }

            _lifetimeCache[type] = StateLifetime.Persistent;
            return StateLifetime.Persistent;
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
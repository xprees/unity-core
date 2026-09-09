using NUnit.Framework;
using UnityEngine;

namespace Xprees.Core.Tests
{
    public class StateSnapshotServiceTests
    {
        [StatefulLifetime(StateLifetime.Scenario)]
        private class TestStatefulSO : ScriptableObject, IRuntimeStateOwner
        {
            public int intValue = 10;
            public string stringValue = "initial";
            public bool transientFlag = false;

            public void ClearTransientState()
            {
                transientFlag = false;
            }
        }

        [StatefulLifetime(StateLifetime.Scenario)]
        private class TestSnapshotIgnoreSO : ScriptableObject
        {
            public int capturedValue = 10;
            [SnapshotIgnore] public string ignoredValue = "initial";
        }

        private class TestPlainSO : ScriptableObject
        {
            public int count = 5;
        }

        [StatelessAsset]
        private class TestStatelessSO : ScriptableObject
        {
            public int count = 5;
        }

        [Stateless]
        private class TestStatelessAliasSO : ScriptableObject
        {
            public int count = 5;
        }

        [StatefulLifetime(StateLifetime.Persistent)]
        private class TestPersistentSO : ScriptableObject
        {
            public int score = 100;
        }

        [SetUp]
        public void Setup()
        {
            StateSnapshotService.ClearAllSnapshots();
        }

        [TearDown]
        public void TearDown()
        {
            StateSnapshotService.ClearAllSnapshots();
        }

        [Test]
        public void EnsureCaptured_And_Restore_RevertsMutatedState()
        {
            var so = ScriptableObject.CreateInstance<TestStatefulSO>();
            so.intValue = 42;
            so.stringValue = "pristine";

            StateSnapshotService.EnsureCaptured(so);
            Assert.IsTrue(StateSnapshotService.HasSnapshot(so));

            // Mutate
            so.intValue = 999;
            so.stringValue = "corrupted";

            // Restore
            StateSnapshotService.Restore(so);

            Assert.AreEqual(42, so.intValue);
            Assert.AreEqual("pristine", so.stringValue);

            Object.DestroyImmediate(so);
        }

        [Test]
        public void SnapshotIgnore_PreservesFieldValueAcrossRestore()
        {
            var so = ScriptableObject.CreateInstance<TestSnapshotIgnoreSO>();
            so.capturedValue = 10;
            so.ignoredValue = "before_capture";

            StateSnapshotService.EnsureCaptured(so);

            // Mutate both
            so.capturedValue = 99;
            so.ignoredValue = "mutated_after_capture";

            StateSnapshotService.Restore(so);

            // capturedValue should be restored to baseline (10)
            Assert.AreEqual(10, so.capturedValue);
            // ignoredValue was excluded from overwrite, so it retains its mutated value ("mutated_after_capture")
            Assert.AreEqual("mutated_after_capture", so.ignoredValue);

            Object.DestroyImmediate(so);
        }

        [Test]
        public void RestoreAll_RestoresAllTrackedTargets()
        {
            var so1 = ScriptableObject.CreateInstance<TestStatefulSO>();
            so1.intValue = 10;
            so1.stringValue = "first";

            var so2 = ScriptableObject.CreateInstance<TestStatefulSO>();
            so2.intValue = 20;
            so2.stringValue = "second";

            StateSnapshotService.EnsureCaptured(so1);
            StateSnapshotService.EnsureCaptured(so2);

            // Mutate both
            so1.intValue = 111;
            so2.intValue = 222;

            // RestoreAll
            var restored = StateSnapshotService.RestoreAll();
            Assert.AreEqual(2, restored);
            Assert.AreEqual(10, so1.intValue);
            Assert.AreEqual(20, so2.intValue);

            Object.DestroyImmediate(so1);
            Object.DestroyImmediate(so2);
        }

        [Test]
        public void PlainScriptableObject_WithoutAttributeOrDescriptionBaseSO_IsStatelessAndIgnored()
        {
            var so = ScriptableObject.CreateInstance<TestPlainSO>();
            Assert.IsTrue(so.IsStateless());
            Assert.AreEqual(StateLifetime.Persistent, so.GetStateLifetime());
            Assert.IsFalse(StateSnapshotService.EnsureCaptured(so));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void StatelessAsset_IsIgnoredBySnapshotEngine()
        {
            var so = ScriptableObject.CreateInstance<TestStatelessSO>();
            so.count = 50;

            StateSnapshotService.EnsureCaptured(so);
            Assert.IsFalse(StateSnapshotService.HasSnapshot(so));

            Object.DestroyImmediate(so);
        }

        [Test]
        public void StatelessAttribute_IsIgnoredBySnapshotEngine()
        {
            var so = ScriptableObject.CreateInstance<TestStatelessAliasSO>();
            so.count = 50;

            StateSnapshotService.EnsureCaptured(so);
            Assert.IsFalse(StateSnapshotService.HasSnapshot(so));

            Object.DestroyImmediate(so);
        }

        [Test]
        public void PersistentLifetime_IsIgnoredBySnapshotEngine()
        {
            var so = ScriptableObject.CreateInstance<TestPersistentSO>();
            so.score = 200;

            StateSnapshotService.EnsureCaptured(so);
            Assert.IsFalse(StateSnapshotService.HasSnapshot(so));

            Object.DestroyImmediate(so);
        }

        [Test]
        public void ClearTransientState_ResetsNonSerializedFlags()
        {
            var so = ScriptableObject.CreateInstance<TestStatefulSO>();
            so.transientFlag = true;

            so.ClearTransientState();
            Assert.IsFalse(so.transientFlag);

            Object.DestroyImmediate(so);
        }
    }
}
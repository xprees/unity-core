using NUnit.Framework;
using UnityEngine;

namespace Xprees.Core.Tests
{
    public class StateSnapshotServiceTests
    {
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

        [StatelessAsset]
        private class TestStatelessSO : ScriptableObject
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
        public void StatelessAsset_IsIgnoredBySnapshotEngine()
        {
            var so = ScriptableObject.CreateInstance<TestStatelessSO>();
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
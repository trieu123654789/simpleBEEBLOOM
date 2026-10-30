using UnityEngine;

namespace LetItBee.HoneyPilot
{
    public sealed partial class BeeHoneyFeedback
    {
        readonly BeeHoneyPresentationLibrary presentation;
        readonly BeeHoneyEffectRig flowerRig, carryRig, hiveRig;
        readonly bool ownsDropletMesh;

        static BeeHoneyEffectRig InstantiateRig(BeeHoneyEffectRig prefab, Transform parent)
        {
            var rig = Object.Instantiate(prefab, parent, false);
            rig.transform.localPosition = Vector3.zero;
            rig.transform.localRotation = Quaternion.identity;
            rig.transform.localScale = Vector3.one;
            return rig;
        }

        LineRenderer PrefabLine(string objectName)
        {
            var line = flowerRig != null ? flowerRig.FindLine(objectName) : null;
            if (line == null && carryRig != null) line = carryRig.FindLine(objectName);
            if (line == null && hiveRig != null) line = hiveRig.FindLine(objectName);
            if (line != null) { line.sharedMaterial = ribbonMaterial; line.enabled = false; }
            return line;
        }

        AudioSource PrefabAudio(string objectName) => carryRig != null ? carryRig.FindAudio(objectName) : null;
    }
}

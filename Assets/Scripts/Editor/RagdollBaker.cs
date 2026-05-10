#if UNITY_EDITOR
using System.Collections.Generic;
using TacticalRPG.ThirdPerson;
using UnityEditor;
using UnityEngine;

namespace TacticalRPG.EditorTools
{
    /// <summary>
    /// One-shot baker for the Tier-3 active ragdoll setup. Call
    /// <see cref="Bake(GameObject)"/> on a humanoid GameObject (HeroPrefab
    /// instance, or the source prefab itself) and it adds:
    ///
    ///  • A <see cref="Rigidbody"/> on each ragdoll bone (12 by default —
    ///    Hips, Spine, Spine1, Head, both Arms / ForeArms, both UpperLegs /
    ///    Lower Legs).
    ///  • A primitive collider per bone (capsule on limbs/torso, sphere on
    ///    head) sized to the bone's length and rotated to match its axis.
    ///  • A <see cref="ConfigurableJoint"/> on every bone except Hips,
    ///    parented to the parent bone's rigidbody. Translation locked,
    ///    angular limits anatomical, slerpDrive configured.
    ///  • An <see cref="ActiveRagdollDriver"/> on the root with the bone
    ///    list populated.
    ///
    /// Pairs of adjacent ragdoll bones have <see cref="Physics.IgnoreCollision"/>
    /// applied so the body doesn't punch itself.
    ///
    /// Re-runnable: existing rigidbodies / joints / colliders on the bone
    /// list are removed first, then re-baked. Other components on the bones
    /// (animator weights, fingers, etc.) are untouched.
    /// </summary>
    public static class RagdollBaker
    {
        // ── Spec ────────────────────────────────────────────────────

        // The 12-bone ragdoll. parent is the joint connectedBody (humanoid bone).
        // "ColliderKind" defines the collider primitive; "Mass" is in kg.
        private struct BoneSpec
        {
            public HumanBodyBones bone;
            public HumanBodyBones jointParent;     // the bone this one's joint connects TO. Hips: same as self → no joint.
            public ColliderKind collider;
            public float mass;
            public float spring;
            public float damper;
            // Anatomical angular limits (degrees). Used if collider != none.
            public float angXLow, angXHigh, angYLim, angZLim;
            public BoneSpec(HumanBodyBones b, HumanBodyBones par, ColliderKind c,
                            float m, float spr, float d,
                            float xL, float xH, float y, float z)
            {
                bone = b; jointParent = par; collider = c;
                mass = m; spring = spr; damper = d;
                angXLow = xL; angXHigh = xH; angYLim = y; angZLim = z;
            }
        }

        private enum ColliderKind { Capsule, Sphere, Box }

        // 12-bone spec with anatomical limits + reasonable initial spring
        // values. The spring numbers are deliberately on the high side —
        // joints lock in close to animation. Tune via Inspector.
        private static readonly BoneSpec[] Spec = new[]
        {
            // bone, parent, collider, mass, spring, damper, angXLow, angXHigh, angY, angZ
            new BoneSpec(HumanBodyBones.Hips,         HumanBodyBones.Hips,        ColliderKind.Box,     12f, 0,     0,     0,    0,   0,   0),
            new BoneSpec(HumanBodyBones.Spine,        HumanBodyBones.Hips,        ColliderKind.Box,      8f, 12000, 400, -25,   25,  20,  20),
            new BoneSpec(HumanBodyBones.Chest,        HumanBodyBones.Spine,       ColliderKind.Box,      8f, 12000, 400, -25,   25,  20,  20),
            new BoneSpec(HumanBodyBones.Head,         HumanBodyBones.Chest,       ColliderKind.Sphere,   4f,  6000, 200, -30,   30,  35,  50),
            new BoneSpec(HumanBodyBones.LeftUpperArm, HumanBodyBones.Chest,       ColliderKind.Capsule,  3f,  4000, 130, -90,   90,  90,  60),
            new BoneSpec(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftUpperArm,ColliderKind.Capsule,  2f,  3000, 100,   0,  150,   1,   1),
            new BoneSpec(HumanBodyBones.RightUpperArm,HumanBodyBones.Chest,       ColliderKind.Capsule,  3f,  4000, 130, -90,   90,  90,  60),
            new BoneSpec(HumanBodyBones.RightLowerArm,HumanBodyBones.RightUpperArm,ColliderKind.Capsule, 2f,  3000, 100,   0,  150,   1,   1),
            new BoneSpec(HumanBodyBones.LeftUpperLeg, HumanBodyBones.Hips,        ColliderKind.Capsule,  8f, 14000, 450, -30,   90,  45,  30),
            new BoneSpec(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftUpperLeg,ColliderKind.Capsule,  4f,  9000, 300,   0,  150,   1,   1),
            new BoneSpec(HumanBodyBones.RightUpperLeg,HumanBodyBones.Hips,        ColliderKind.Capsule,  8f, 14000, 450, -30,   90,  45,  30),
            new BoneSpec(HumanBodyBones.RightLowerLeg,HumanBodyBones.RightUpperLeg,ColliderKind.Capsule, 4f,  9000, 300,   0,  150,   1,   1),
        };

        [MenuItem("TacticalRPG/Ragdoll/Bake On Selected")]
        public static void BakeOnSelected()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogError("[RagdollBaker] Select a GameObject with an Animator first.");
                return;
            }
            Bake(go);
        }

        public static void Bake(GameObject root)
        {
            var animator = root.GetComponentInChildren<Animator>();
            if (animator == null || !animator.isHuman)
            {
                Debug.LogError("[RagdollBaker] Target needs a Humanoid Animator.");
                return;
            }

            // First pass — clean any prior bake on the spec bones.
            foreach (var s in Spec)
            {
                var b = animator.GetBoneTransform(s.bone);
                if (b == null) continue;
                StripPriorBake(b.gameObject);
            }
            var oldDriver = root.GetComponentInChildren<ActiveRagdollDriver>();
            if (oldDriver != null) Object.DestroyImmediate(oldDriver);

            // Second pass — add Rigidbody + collider per bone.
            var bonesByEnum = new Dictionary<HumanBodyBones, Transform>();
            foreach (var s in Spec)
            {
                var t = animator.GetBoneTransform(s.bone);
                if (t == null) { Debug.LogWarning($"[RagdollBaker] Bone {s.bone} missing — skipped."); continue; }
                bonesByEnum[s.bone] = t;
                AttachBody(t.gameObject, s.mass);
                AttachCollider(t.gameObject, s, animator);
            }

            // Third pass — joints. Hips = root (no joint). Others connect to parent's body.
            var entries = new List<ActiveRagdollDriver.BoneEntry>();
            foreach (var s in Spec)
            {
                if (!bonesByEnum.TryGetValue(s.bone, out var t)) continue;

                ConfigurableJoint joint = null;
                if (s.bone != s.jointParent && bonesByEnum.TryGetValue(s.jointParent, out var pT))
                {
                    var parentBody = pT.GetComponent<Rigidbody>();
                    joint = AttachJoint(t.gameObject, parentBody, s);
                }

                entries.Add(new ActiveRagdollDriver.BoneEntry
                {
                    boneName             = s.bone.ToString(),
                    bone                 = t,
                    body                 = t.GetComponent<Rigidbody>(),
                    joint                = joint,
                    initialLocalRotation = t.localRotation,
                    baselineSpring       = s.spring,
                    baselineDamper       = s.damper,
                });
            }

            // Fourth pass — disable self-collisions among ragdoll bones (Physics.IgnoreCollision is per-instance, not per-prefab; persists in scene).
            var allColliders = new List<Collider>();
            foreach (var s in Spec)
            {
                if (!bonesByEnum.TryGetValue(s.bone, out var t)) continue;
                var c = t.GetComponent<Collider>();
                if (c != null) allColliders.Add(c);
            }
            for (int i = 0; i < allColliders.Count; i++)
                for (int j = i + 1; j < allColliders.Count; j++)
                    Physics.IgnoreCollision(allColliders[i], allColliders[j], true);

            // Fifth pass — driver on the root.
            var driver = root.AddComponent<ActiveRagdollDriver>();
            driver.ConfigureFromBake(animator, entries);

            // Mark the scene/prefab dirty so the bake survives a save.
            EditorUtility.SetDirty(root);

            Debug.Log($"[RagdollBaker] Baked {entries.Count} bones onto '{root.name}'.");
        }

        // ── Helpers ─────────────────────────────────────────────────

        private static void StripPriorBake(GameObject go)
        {
            var j = go.GetComponent<ConfigurableJoint>();    if (j != null) Object.DestroyImmediate(j);
            var c = go.GetComponent<CharacterJoint>();       if (c != null) Object.DestroyImmediate(c);
            var rb = go.GetComponent<Rigidbody>();           if (rb != null) Object.DestroyImmediate(rb);
            // Don't strip arbitrary colliders — only the kind we add.
            var caps = go.GetComponent<CapsuleCollider>();   if (caps != null) Object.DestroyImmediate(caps);
            var sph  = go.GetComponent<SphereCollider>();    if (sph != null)  Object.DestroyImmediate(sph);
            var box  = go.GetComponent<BoxCollider>();       if (box != null)  Object.DestroyImmediate(box);
        }

        private static void AttachBody(GameObject go, float mass)
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.linearDamping = 0.05f;
            rb.angularDamping = 0.5f;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        private static void AttachCollider(GameObject go, BoneSpec spec, Animator animator)
        {
            // Bone "length" is the distance to the spec's natural tail. We
            // use a reasonable child for each bone; falls back to a default
            // if the child can't be located.
            float bonelen = EstimateBoneLength(go.transform, spec.bone, animator);

            switch (spec.collider)
            {
                case ColliderKind.Capsule:
                {
                    var c = go.AddComponent<CapsuleCollider>();
                    c.radius = Mathf.Max(0.04f, bonelen * 0.25f);
                    c.height = Mathf.Max(0.1f, bonelen);
                    c.direction = GuessCapsuleAxis(go.transform, spec.bone, animator);
                    c.center = OffsetTowardChild(go.transform, spec.bone, animator) * (bonelen * 0.5f);
                    break;
                }
                case ColliderKind.Sphere:
                {
                    var c = go.AddComponent<SphereCollider>();
                    c.radius = Mathf.Max(0.08f, bonelen * 0.45f);
                    c.center = OffsetTowardChild(go.transform, spec.bone, animator) * (bonelen * 0.5f);
                    break;
                }
                case ColliderKind.Box:
                {
                    var c = go.AddComponent<BoxCollider>();
                    // Torso box: roughly shoulder-wide × spine-long × half-as-deep.
                    var size = new Vector3(0.30f, Mathf.Max(0.15f, bonelen), 0.20f);
                    c.size = size;
                    c.center = OffsetTowardChild(go.transform, spec.bone, animator) * (bonelen * 0.5f);
                    break;
                }
            }
        }

        private static float EstimateBoneLength(Transform bone, HumanBodyBones b, Animator anim)
        {
            // For each ragdoll bone, pick a sensible "child tip" to measure to.
            HumanBodyBones tip = b switch
            {
                HumanBodyBones.Hips         => HumanBodyBones.Spine,
                HumanBodyBones.Spine        => HumanBodyBones.Chest,
                HumanBodyBones.Chest        => HumanBodyBones.Head, // approx
                HumanBodyBones.Head         => HumanBodyBones.Head, // self → fall back
                HumanBodyBones.LeftUpperArm => HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftLowerArm => HumanBodyBones.LeftHand,
                HumanBodyBones.RightUpperArm=> HumanBodyBones.RightLowerArm,
                HumanBodyBones.RightLowerArm=> HumanBodyBones.RightHand,
                HumanBodyBones.LeftUpperLeg => HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftLowerLeg => HumanBodyBones.LeftFoot,
                HumanBodyBones.RightUpperLeg=> HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightLowerLeg=> HumanBodyBones.RightFoot,
                _ => b,
            };
            var tipT = anim.GetBoneTransform(tip);
            if (tipT == null || tipT == bone) return 0.25f;
            return Vector3.Distance(bone.position, tipT.position);
        }

        private static int GuessCapsuleAxis(Transform bone, HumanBodyBones b, Animator anim)
        {
            // Determine which local axis points down the bone toward its tip.
            HumanBodyBones tip = b switch
            {
                HumanBodyBones.LeftUpperArm => HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftLowerArm => HumanBodyBones.LeftHand,
                HumanBodyBones.RightUpperArm=> HumanBodyBones.RightLowerArm,
                HumanBodyBones.RightLowerArm=> HumanBodyBones.RightHand,
                HumanBodyBones.LeftUpperLeg => HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftLowerLeg => HumanBodyBones.LeftFoot,
                HumanBodyBones.RightUpperLeg=> HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightLowerLeg=> HumanBodyBones.RightFoot,
                _ => b,
            };
            var tipT = anim.GetBoneTransform(tip);
            if (tipT == null || tipT == bone) return 1;
            Vector3 localDir = bone.InverseTransformDirection((tipT.position - bone.position).normalized);
            float ax = Mathf.Abs(localDir.x), ay = Mathf.Abs(localDir.y), az = Mathf.Abs(localDir.z);
            if (ax >= ay && ax >= az) return 0;
            if (ay >= ax && ay >= az) return 1;
            return 2;
        }

        private static Vector3 OffsetTowardChild(Transform bone, HumanBodyBones b, Animator anim)
        {
            HumanBodyBones tip = b switch
            {
                HumanBodyBones.Hips         => HumanBodyBones.Spine,
                HumanBodyBones.Spine        => HumanBodyBones.Chest,
                HumanBodyBones.Chest        => HumanBodyBones.Head,
                HumanBodyBones.Head         => HumanBodyBones.Head,
                HumanBodyBones.LeftUpperArm => HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftLowerArm => HumanBodyBones.LeftHand,
                HumanBodyBones.RightUpperArm=> HumanBodyBones.RightLowerArm,
                HumanBodyBones.RightLowerArm=> HumanBodyBones.RightHand,
                HumanBodyBones.LeftUpperLeg => HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftLowerLeg => HumanBodyBones.LeftFoot,
                HumanBodyBones.RightUpperLeg=> HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightLowerLeg=> HumanBodyBones.RightFoot,
                _ => b,
            };
            var tipT = anim.GetBoneTransform(tip);
            if (tipT == null || tipT == bone) return Vector3.zero;
            return bone.InverseTransformDirection((tipT.position - bone.position).normalized);
        }

        private static ConfigurableJoint AttachJoint(GameObject go, Rigidbody parentBody, BoneSpec spec)
        {
            var joint = go.AddComponent<ConfigurableJoint>();
            joint.connectedBody = parentBody;
            joint.autoConfigureConnectedAnchor = true;

            // Translation locked at the bone's origin → the joint behaves as a ball-and-socket pivot.
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            // Anatomical angular limits (degrees, symmetric on Y/Z).
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;

            joint.lowAngularXLimit  = new SoftJointLimit { limit = spec.angXLow };
            joint.highAngularXLimit = new SoftJointLimit { limit = spec.angXHigh };
            joint.angularYLimit     = new SoftJointLimit { limit = spec.angYLim };
            joint.angularZLimit     = new SoftJointLimit { limit = spec.angZLim };

            // Drive in joint-local space — the driver writes targetRotation each FixedUpdate.
            joint.rotationDriveMode = RotationDriveMode.Slerp;
            joint.slerpDrive = new JointDrive
            {
                positionSpring = spec.spring,
                positionDamper = spec.damper,
                maximumForce   = Mathf.Infinity,
            };

            // Project ON, so the joint snaps if the limit is heavily violated
            // (prevents the body exploding under big impulses).
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.1f;
            joint.projectionAngle    = 5f;

            return joint;
        }
    }
}
#endif

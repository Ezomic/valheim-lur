using System.IO;
using UnityEngine;
using Ezomic.Shared;

namespace Lur
{
    /// <summary>
    /// The horn itself.
    ///
    /// Cloned from a vanilla material item rather than assembled from nothing, because an item
    /// needs a great deal of machinery that has nothing to do with what it looks like - a
    /// ZNetView, an ItemDrop, a Rigidbody, colliders, the float-in-water behaviour and the
    /// auto-pickup radius. All of that comes across; only the mesh, the name and the icon
    /// change. Building from scratch is the right call when you do <i>not</i> want the donor's
    /// machinery, and here every bit of it is wanted.
    ///
    /// The model is hand-built and read off disk as an .obj beside the DLL, not a vanilla prop
    /// wearing a new name. The icon is a PNG rendered in Blender: Valheim builds item icons
    /// from a camera rig in the editor and there is none of that at runtime, so Sprite.Create
    /// over a loaded texture is the whole of it.
    /// </summary>
    internal static class LurItem
    {
        /// <summary>
        /// The network identity, and what the store row resolves. ZNetScene and ObjectDB both
        /// key on name.GetStableHashCode(), and a saved ZDO or a saved inventory stores that
        /// hash - so renaming this orphans every horn already in a world, silently and
        /// permanently. Fixed at first ship. The display name is a config entry instead.
        /// </summary>
        public const string Name = "Lur";

        private const string Mesh = "lur.obj";
        private const string Icon = "lur.png";

        internal static GameObject Build()
        {
            string directory = Path.GetDirectoryName(typeof(LurItem).Assembly.Location);

            ModelData model = ObjMesh.Load(Path.Combine(directory, Mesh));
            if (model == null)
            {
                LurPlugin.Log.LogError("No " + Mesh + " beside the dll - cannot build " + Name
                                       + ". The mod will keep retrying, and Hildir will not "
                                       + "stock the horn until it succeeds.");
                return null;
            }

            GameObject source = Donor();
            if (source == null) return null;

            // The hidden holder and the init suppression both come from Prefabs. The
            // suppression is the load-bearing half: a clone taken under an active parent runs
            // its ZNetView's Awake and tries to network-register itself while half-built.
            GameObject clone = Prefabs.Clone(source, Name);
            if (clone == null) return null;

            clone.transform.localRotation = Quaternion.identity;

            Visual(clone, model);

            ItemDrop drop = clone.GetComponent<ItemDrop>();
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null)
            {
                LurPlugin.Log.LogError("Donor " + source.name + " has no usable ItemDrop.");
                return null;
            }

            // Instantiate deep-copies serialized fields and SharedData is [Serializable], so
            // this is our own copy rather than the donor's. Writing to the donor's would
            // rename every one of those in the world.
            ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;

            shared.m_name = LurConfig.ItemName.Value;
            // One line, by request. The tooltip used to carry the honesty lines too - that it
            // wakes the boss and nothing else, and that what was taken stays taken - and those
            // now live in the README alone. Worth knowing if a player ever asks why their
            // chests are still empty: the item no longer says.
            shared.m_description = "A coiled horn of bone and iron, worn smooth at the mouth.";

            // Material, not Consumable: Consumable routes UseItem into ConsumeItem and the
            // eat animation, and Consumable is also the branch that destroys the item whatever
            // the handler returned. A Material falls through to the branch this mod's prefix
            // intercepts, and the horn is then spent by Lur itself, only on success.
            shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
            shared.m_maxStackSize = 10;
            shared.m_weight = 1.0f;
            shared.m_teleportable = true;
            shared.m_questItem = false;

            Sprite icon = Icons.Load(Icon, Name);
            if (icon != null) shared.m_icons = new[] { icon };

            drop.m_itemData.m_stack = 1;
            drop.m_itemData.m_dropPrefab = clone;

            LurPlugin.Log.LogInfo("Built " + Name + " from " + source.name + ".");
            return clone;
        }

        /// <summary>
        /// A vanilla item to take the machinery from. Configurable because which donor yields
        /// a clean material item is a question for the game rather than a guess, and because a
        /// game update can retire a prefab.
        /// </summary>
        private static GameObject Donor()
        {
            ZNetScene scene = ZNetScene.instance;

            foreach (string name in new[] { LurConfig.ItemDonor.Value, "SurtlingCore", "Wood" })
            {
                if (string.IsNullOrEmpty(name)) continue;

                GameObject found = scene.GetPrefab(name);
                if (found != null) return found;

                LurPlugin.LogOnce("Item donor '" + name + "' does not exist.");
            }

            return null;
        }

        private static void Visual(GameObject clone, ModelData model)
        {
            // The components, not the GameObjects they sit on. On some donors the collider
            // shares an object with the renderer, so destroying the object takes the item's
            // collision with it - and an ItemDrop carries a Rigidbody, so a dropped one with
            // nothing to rest on simply keeps falling and is gone. Removing two components
            // leaves the hierarchy and the colliders exactly where the ItemDrop expects them.
            foreach (MeshRenderer renderer in clone.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null) continue;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null) Object.DestroyImmediate(filter);

                Object.DestroyImmediate(renderer);
            }

            // Then the light and the sparkle, which are not the mesh and do not go with it.
            //
            // A dropped horn glowed. The default donor is SurtlingCore, and what makes a surtling
            // core visibly glow is a child Light plus a particle flare - the emission is a
            // separate object, not the material - so stripping renderers left the whole light rig
            // behind on an item made of bone. This is the trap CLAUDE.md records for Strip(),
            // which removes MonoBehaviours and colliders and deliberately not ParticleSystems.
            //
            // Components rather than their GameObjects, for the same reason as above: on some
            // donors those children carry other things. A Light with no Light component emits
            // nothing, and that is the whole requirement.
            //
            // Done here rather than by choosing a duller donor, because the donor is a config
            // entry and the next one somebody picks should not be able to reintroduce this.
            foreach (Light light in clone.GetComponentsInChildren<Light>(true))
                if (light != null) Object.DestroyImmediate(light);

            foreach (ParticleSystem particles in clone.GetComponentsInChildren<ParticleSystem>(true))
                if (particles != null) Object.DestroyImmediate(particles);

            foreach (ParticleSystemRenderer drawn
                     in clone.GetComponentsInChildren<ParticleSystemRenderer>(true))
                if (drawn != null) Object.DestroyImmediate(drawn);

            var visual = new GameObject("lur_visual");
            visual.transform.SetParent(clone.transform, false);

            visual.AddComponent<MeshFilter>().sharedMesh = model.Mesh;

            MeshRenderer added = visual.AddComponent<MeshRenderer>();
            added.sharedMaterials = Skins.Skin(model.Groups);
            Skins.Remap(model.Mesh, model.Groups);

            Ground(clone, model);
        }

        /// <summary>
        /// Makes sure it has something to land on. Insurance behind the stripping rule above
        /// rather than the fix itself: this catches a donor that never had a collider. The
        /// failure it guards against is unusually bad for an item - it falls through the world
        /// with no message and nothing to pick back up.
        /// </summary>
        private static void Ground(GameObject clone, ModelData model)
        {
            if (clone.GetComponentInChildren<Collider>(true) != null) return;
            if (model.Mesh == null) return;

            Bounds bounds = model.Mesh.bounds;

            BoxCollider box = clone.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;

            LurPlugin.LogOnce("Donor left no collider on " + Name + " - added one from the "
                              + "mesh bounds so it does not fall through the world.");
        }
    }
}

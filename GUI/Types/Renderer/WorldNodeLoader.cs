using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using GUI.Utils;
using ValveResourceFormat;
using ValveResourceFormat.IO;
using ValveResourceFormat.ResourceTypes;
using ValveResourceFormat.Serialization;

namespace GUI.Types.Renderer
{
    class WorldNodeLoader
    {
        private readonly WorldNode node;
        private readonly VrfGuiContext guiContext;
        public string[] LayerNames { get; }

        public WorldNodeLoader(VrfGuiContext vrfGuiContext, WorldNode node)
        {
            this.node = node;
            guiContext = vrfGuiContext;

            if (node.Data.ContainsKey("m_layerNames"))
            {
                LayerNames = node.Data.GetArray<string>("m_layerNames");
            }
            else
            {
                LayerNames = [];
            }
        }

        public static void PreloadInnerFilesForResource(Resource resource, IFileLoader loader)
        {
            if (resource.ResourceType == ResourceType.Model)
            {
                var model = (Model)resource.DataBlock;
                model.GetAllAnimations(loader); // This call will cache into CachedAnimations

                // TODO: subpar
                var meshNamesForLod1 = model.GetReferenceMeshNamesAndLoD().Where(m => (m.LoDMask & 1) != 0).ToList();
                foreach (var refMesh in meshNamesForLod1)
                {
                    var newResource = loader.LoadFileCompiled(refMesh.MeshName);

                    if (newResource != null)
                    {
                        PreloadInnerFilesForResource(newResource, loader);
                    }
                }
            }
            else if (resource.ResourceType == ResourceType.Mesh)
            {
                var mesh = (Mesh)resource.DataBlock;
                var vbib = mesh.VBIB; // Access vbib to force decode
            }
        }

        public void Load(Scene scene)
        {
            var i = 0;
            var defaultLightingOrigin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            // Output is WorldNode_t we need to iterate m_sceneObjects inside it

            // TODO: Remove this, we can use file loader cache instead
            var resources = new ConcurrentDictionary<string, Resource>(
                concurrencyLevel: Environment.ProcessorCount,
                capacity: Math.Max(node.AggregateSceneObjects.Count, node.SceneObjects.Count)
            );

            Parallel.ForEach(node.SceneObjects, (sceneObject) =>
            {
                var renderableModel = sceneObject.GetProperty<string>("m_renderableModel");
                var renderable = sceneObject.GetProperty<string>("m_renderable");

                if (renderableModel != null)
                {
                    var newResource = guiContext.LoadFileCompiled(renderableModel);

                    if (newResource != null)
                    {
                        PreloadInnerFilesForResource(newResource, guiContext.FileLoader);
                        resources.TryAdd(renderableModel, newResource);
                    }
                }

                if (!string.IsNullOrEmpty(renderable))
                {
                    var newResource = guiContext.LoadFileCompiled(renderable);

                    if (newResource != null)
                    {
                        PreloadInnerFilesForResource(newResource, guiContext.FileLoader);
                        resources.TryAdd(renderable, newResource);
                    }
                }
            });

            foreach (var sceneObject in node.SceneObjects)
            {
                var layerIndex = (int)(node.SceneObjectLayerIndices?[i++] ?? -1);

                // m_vCubeMapOrigin in older files
                var lightingOrigin = sceneObject.ContainsKey("m_vLightingOrigin") ? sceneObject.GetSubCollection("m_vLightingOrigin").ToVector3() : defaultLightingOrigin;
                var overlayRenderOrder = sceneObject.GetInt32Property("m_nOverlayRenderOrder");
                var cubeMapPrecomputedHandshake = sceneObject.GetInt32Property("m_nCubeMapPrecomputedHandshake");
                var lightProbeVolumePrecomputedHandshake = sceneObject.GetInt32Property("m_nLightProbeVolumePrecomputedHandshake");

                // sceneObject is SceneObject_t
                var renderableModel = sceneObject.GetProperty<string>("m_renderableModel");
                var matrix = sceneObject.GetArray("m_vTransform").ToMatrix4x4();

                var tintColor = sceneObject.GetSubCollection("m_vTintColor").ToVector4();
                if (tintColor.W == 0)
                {
                    // Ignoring tintColor, it will fuck things up.
                    tintColor = Vector4.One;
                }

                if (renderableModel != null && resources.TryGetValue(renderableModel, out var newResource))
                {
                    var modelNode = new ModelSceneNode(scene, (Model)newResource.DataBlock, null, optimizeForMapLoad: true)
                    {
                        Transform = matrix,
                        Tint = tintColor,
                        LayerName = layerIndex > -1 ? node.LayerNames[layerIndex] : "No layer",
                        Name = renderableModel,
                        LightingOrigin = lightingOrigin == defaultLightingOrigin ? null : lightingOrigin,
                        OverlayRenderOrder = overlayRenderOrder,
                        CubeMapPrecomputedHandshake = cubeMapPrecomputedHandshake,
                        LightProbeVolumePrecomputedHandshake = lightProbeVolumePrecomputedHandshake,
                    };

                    scene.Add(modelNode, false);
                }

                var renderable = sceneObject.GetProperty<string>("m_renderable");

                if (!string.IsNullOrEmpty(renderable) && resources.TryGetValue(renderable, out newResource))
                {
                    var meshNode = new MeshSceneNode(scene, (Mesh)newResource.DataBlock, 0)
                    {
                        Transform = matrix,
                        Tint = tintColor,
                        LayerName = layerIndex > -1 ? node.LayerNames[layerIndex] : "No layer",
                        Name = renderable,
                        CubeMapPrecomputedHandshake = cubeMapPrecomputedHandshake,
                        LightProbeVolumePrecomputedHandshake = lightProbeVolumePrecomputedHandshake,
                    };

                    scene.Add(meshNode, false);
                }
            }

            resources.Clear();

            Parallel.ForEach(node.AggregateSceneObjects, (sceneObject) =>
            {
                var renderableModel = sceneObject.GetProperty<string>("m_renderableModel");

                if (renderableModel != null)
                {
                    var newResource = guiContext.LoadFileCompiled(renderableModel);

                    if (newResource != null)
                    {
                        PreloadInnerFilesForResource(newResource, guiContext.FileLoader);
                        resources.TryAdd(renderableModel, newResource);
                    }
                }
            });

            foreach (var sceneObject in node.AggregateSceneObjects)
            {
                var renderableModel = sceneObject.GetProperty<string>("m_renderableModel");

                if (renderableModel != null && resources.TryGetValue(renderableModel, out var newResource))
                {
                    var layerIndex = sceneObject.GetIntegerProperty("m_nLayer");
                    var aggregate = new SceneAggregate(scene, (Model)newResource.DataBlock)
                    {
                        LayerName = node.LayerNames[(int)layerIndex],
                        Name = renderableModel,
                    };

                    scene.Add(aggregate, false);
                    foreach (var fragment in aggregate.CreateFragments(sceneObject))
                    {
                        scene.Add(fragment, false);
                    }
                }
            }
        }
    }
}

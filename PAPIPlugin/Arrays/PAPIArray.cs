#region Usings

using System;
using PAPIPlugin.Impl;
using PAPIPlugin.Interfaces;
using PAPIPlugin.Internal;
using UnityEngine;
using Object = UnityEngine.Object;

#endregion

namespace PAPIPlugin.Arrays
{
    public class PAPIArray : AbstractLightArray, IConfigNode
    {
        public const int DefaultPartCount = 4;

        public const float DefaultLightRadius = 10.0f;

        public const double DefaultTargetGlidePath = 6;

        /// <summary>
        ///     If the difference of the gliepath from the target is more than this the whole array will show either red or white.
        /// </summary>
        public const double DefaultGlideslopeTolerance = 1.5;

        private GameObject _papiGameObject;

        private GameObject[] _partObjects;

        private Renderer[] _partRenderers;

        private Light[] _fallbackLights;

        private Vector3d _relativeSurfacePosition;

        public PAPIArray()
        {
            TargetGlideslope = DefaultTargetGlidePath;
            GlideslopeTolerance = DefaultGlideslopeTolerance;

            EnabledChanged += (sender, args) =>
            {
                if (Enabled)
                {
                    return;
                }

                if (_partObjects == null)
                {
                    return;
                }

                foreach (var partObject in _partObjects)
                {
                    partObject.SetActive(false);
                }
            };
        }

        public double GlideslopeTolerance { get; set; }

        public double TargetGlideslope { get; set; }

        public double Longitude { get; set; }

        public double Latitude { get; set; }

        public double Heading { get; set; }

        public double HeightAboveTerrain { get; set; }

        public int PartCount { get; set; }

        public float LightRadius { get; set; }

        public float LightDistance { get; set; }

        #region IConfigNode Members

        public static PositionDecision positionDecision = PositionDecision.Auto;

        public void Load(ConfigNode node)
        {
            GlideslopeTolerance = node.ConvertValue("GlideslopeTolerance", DefaultGlideslopeTolerance);
            TargetGlideslope = node.ConvertValue("TargetGlideslope", DefaultTargetGlidePath);
            HeightAboveTerrain = node.ConvertValue("HeightAboveTerrain", 0);
            PartCount = node.ConvertValue("PartCount", DefaultPartCount);
            LightRadius = node.ConvertValue("LightRadius", DefaultLightRadius);
            LightDistance = node.ConvertValue("LightDistance", LightRadius * 0.5f);

            try
            {
                Longitude = node.ConvertValueWithException<double>("Longitude").ClampAndLog(-180, 180);
                Latitude = node.ConvertValueWithException<double>("Latitude").ClampAndLog(-90, 90);

                var headingDeg = node.ConvertValueWithException<double>("Heading").ClampAndLog(0, 360);
                Heading = (headingDeg / 180) * Math.PI;
            }
            catch (FormatException e)
            {
                Util.LogWarning(e.Message);
            }
        }

        public void Save(ConfigNode node)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override void Destroy()
        {
            if (_partObjects != null)
            {
                foreach (var partObject in _partObjects)
                {
                    Object.Destroy(partObject);
                }
            }

            if (_papiGameObject != null)
            {
                Object.Destroy(_papiGameObject);
            }

            base.Destroy();
        }

        public override void Update()
        {
            if (!Enabled)
            {
                return;
            }

            var currentCamera = HighLogic.LoadedSceneIsFlight ? FlightCamera.fetch.mainCamera : Camera.main;

            if (currentCamera == null || _papiGameObject == null)
            {
                return;
            }
            var relativePosition = _papiGameObject.transform.InverseTransformPoint(currentCamera.transform.position);

            var activeVessel = FlightGlobals.ActiveVessel;

            if (activeVessel != null)
            {
                if ((positionDecision == PositionDecision.Vessel) ||
                        (positionDecision == PositionDecision.Auto && (InternalCamera.Instance == null || !InternalCamera.Instance.isActive)))
                {
                    relativePosition = _papiGameObject.transform.InverseTransformPoint(activeVessel.GetWorldPos3D());
                }
            }

            var normalizedPosition = relativePosition.normalized;
            // As the local normal is (0, 1, 0), y is the result of normal * normalizedPosition.
            var normalDot = normalizedPosition.y;

            var directionDot = normalizedPosition.z;

            var angle = 90 - Math.Acos(normalDot) * (180 / Math.PI);

            var difference = angle - TargetGlideslope;

            for (var i = 0; i < PartCount; i++)
            {
                if (directionDot <= 0)
                {
                    _partObjects[i].SetActive(false);
                }
                else
                {
                    _partObjects[i].SetActive(true);

                    // Use the direction dot for alpha to fade the lights out
                    UpdatePAPIPart(i, difference, directionDot);
                }
            }
        }

        public override void InitializeDisplay(ILightArrayManager arrayManager)
        {
            base.InitializeDisplay(arrayManager);

            InitializePAPIParts(Latitude, Longitude, Heading);
        }

        public override void Initialize(ILightGroup @group)
        {
            base.Initialize(@group);

            @group.GetOrAddTypeManager<PAPITypeManager>();
        }

        /// <summary>
        ///     Initializes the whole array at the given latitude and longitude with the given altitude. The heading is the
        ///     direction to array looks to and should be in the range [0, 2 * PI).
        /// </summary>
        /// <param name="lat">The latitude</param>
        /// <param name="lon">The longitude</param>
        /// <param name="heading">The heading in radians.</param>
        private void InitializePAPIParts(double lat, double lon, double heading)
        {
            var parentBody = ParentGroup.ParentBody;

            var surfaceNormal = parentBody.transform.InverseTransformDirection(parentBody.GetSurfaceNVector(lat, lon));
            var zeroAltSurface = parentBody.transform.InverseTransformPoint(parentBody.GetWorldSurfacePosition(lat, lon, 0));

            var north = Vector3.up * (float) parentBody.Radius; // In body local space, up * radius is the north pole

            var directionToNorth = (north - zeroAltSurface).normalized;

            var orthogonalNorthDir = Orthonormalise(directionToNorth, surfaceNormal);

            var anotherVector = Vector3d.Cross(surfaceNormal, orthogonalNorthDir);

            var headingVector = orthogonalNorthDir * Math.Cos(heading) + anotherVector * Math.Sin(heading);

            _papiGameObject = new GameObject();
            _papiGameObject.transform.parent = parentBody.transform;
            _papiGameObject.transform.localPosition = zeroAltSurface;
            _papiGameObject.transform.localRotation = Quaternion.LookRotation(headingVector, surfaceNormal);

            var maxHeight = double.MinValue;
            _partObjects = new GameObject[PartCount];
            _partRenderers = new Renderer[PartCount];
            _fallbackLights = new Light[PartCount];
            for (var i = 0; i < PartCount; i++)
            {
                var obj = AddPAPIPart(i);

                obj.transform.parent = _papiGameObject.transform;
                obj.transform.localPosition = GetLocalLighPosition(i);

                maxHeight = Math.Max(maxHeight, parentBody.GetSurfaceHeight(Latitude, Longitude));

                _partObjects[i] = obj;
            }

            maxHeight = Math.Max(0, maxHeight);
            _relativeSurfacePosition =
                parentBody.transform.InverseTransformPoint(parentBody.GetWorldSurfacePosition(lat, lon, maxHeight + HeightAboveTerrain + LightRadius));
            _papiGameObject.transform.localPosition = _relativeSurfacePosition;
        }

        /// <summary>
        ///     Gets the local position given a zero-based index.
        /// </summary>
        /// <param name="i">The index of the light, zero-based</param>
        /// <returns>A local position specifying the light position</returns>
        private Vector3 GetLocalLighPosition(int i)
        {
            var countHalf = PartCount / 2.0;

            var offsetMult = (float) (i - countHalf - 0.5);

            var distance = LightRadius + LightDistance;

            return Vector3.right * offsetMult * distance;
        }

        private static Vector3d Orthonormalise(Vector3d direction, Vector3d firstVector)
        {
            // This is basically the first step of a Gram–Schmidt process
            // See http://en.wikipedia.org/wiki/Gram%E2%80%93Schmidt_process

            return direction - Vector3d.Dot(firstVector, direction) * firstVector;
        }

        private GameObject AddPAPIPart(int index)
        {
            var lightMaterial = CreateLightMaterial();
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = string.Format("PAPIPart{0}", index);

            var renderer = obj.GetComponent<Renderer>();
            if (lightMaterial != null)
            {
                renderer.sharedMaterial = lightMaterial;
            }
            else
            {
                renderer.enabled = false;

                var pointLight = obj.AddComponent<Light>();
                pointLight.type = LightType.Point;
                pointLight.range = Mathf.Max(LightRadius * 4.0f, 16.0f);
                pointLight.intensity = 2.0f;
                pointLight.shadows = LightShadows.None;

                _fallbackLights[index] = pointLight;
            }

            obj.transform.localScale = new Vector3(LightRadius, LightRadius, LightRadius);

            var sphereCollider = obj.GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                sphereCollider.enabled = false;
            }

            _partRenderers[index] = renderer;

            return obj;
        }

        private void UpdatePAPIPart(int index, double difference, float alpha)
        {
            var gameObj = _partObjects[index];
            var renderer = _partRenderers[index];
            var fallbackLight = _fallbackLights[index];

            var color = GetArrayPartColor(index, difference);
            color.a = alpha;

            if (renderer != null && renderer.enabled)
            {
                ApplyColor(renderer.material, color);
            }

            if (fallbackLight != null)
            {
                fallbackLight.color = color;
                fallbackLight.intensity = Mathf.Lerp(0.25f, 2.0f, alpha);
            }
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_TintColor"))
            {
                material.SetColor("_TintColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", color);
            }
        }

        private static Material CreateLightMaterial()
        {
            var shader = FindLightShader();
            if (shader == null)
            {
                Util.LogWarning("No supported light shader was found. Falling back to point lights only.");
                return null;
            }

            return new Material(shader);
        }

        private static Shader FindLightShader()
        {
            var shaderNames = new[]
            {
                "Particles/Additive",
                "Legacy Shaders/Particles/Additive",
                "Particles/Standard Unlit",
                "Unlit/Color",
                "Legacy Shaders/Self-Illumin/Diffuse"
            };

            foreach (var shaderName in shaderNames)
            {
                var shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    Util.LogInfo(string.Format("Using shader {0} for PAPI lights.", shaderName));
                    return shader;
                }
            }

            return null;
        }

        private Color GetArrayPartColor(int index, double difference)
        {
            if (difference < -GlideslopeTolerance)
            {
                return Color.red;
            }
            if (difference > GlideslopeTolerance)
            {
                return Color.white;
            }

            // This should map temp into [-1, 1]
            double temp = index - (PartCount / 2);
// ReSharper disable once PossibleLossOfFraction
            temp = temp / (PartCount / 2);

            return temp > difference ? Color.red : Color.white;
        }
    }
}

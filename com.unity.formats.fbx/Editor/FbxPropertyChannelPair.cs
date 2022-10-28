using Autodesk.Fbx;
using System.Collections.Generic;

namespace UnityEditor.Formats.Fbx.Exporter
{
    /// <summary>
    /// Store FBX property name and channel name
    /// Default constructor added because it needs to be called before autoimplemented properties can be assigned. Otherwise we get build errors
    /// </summary>
    struct FbxPropertyChannelPair
    {
        public string Property { get; private set; }
        public string Channel { get; private set; }

        public FbxPropertyChannelPair(string p, string c) : this()
        {
            Property = p;
            Channel = c;
        }

        struct UnityPropertyChannelPair
        {
            public string property;
            public string channel;

            public UnityPropertyChannelPair(string p, string c)
            {
                property = p;
                channel = c;
            }
        }

        /// <summary>
        /// Contains the two lists that map Unity property to FBX property and Unity channel to Fbx channel
        /// for a set of properties.
        /// </summary>
        struct PropertyChannelMap
        {
            public List<(string, string)> MapUnityPropToFbxProp;
            public List<(string, string)> MapUnityChannelToFbxChannel;

            public PropertyChannelMap(List<(string, string)> propertyMap, List<(string, string)> channelMap)
            {
                MapUnityPropToFbxProp = propertyMap;
                MapUnityChannelToFbxChannel = channelMap;
            }

            private string GetFbxValue(string uniValue, List<(string, string)> list)
            {
                return list.Find(x => x.Item1 == uniValue).Item2;
            }

            /// <summary>
            /// Get the Fbx property name for the given Unity property name from the given list.
            /// </summary>
            /// <param name="uniProperty"></param>
            /// <param name="propertyMap"></param>
            /// <returns>The Fbx property name or null if there was no match in the list</returns>
            public string GetFbxProperty(string uniProperty)
            {
                return GetFbxValue(uniProperty, MapUnityPropToFbxProp);
            }

            /// <summary>
            /// Get the Fbx channel name for the given Unity channel from the given list.
            /// </summary>
            /// <param name="uniChannel"></param>
            /// <param name="channelMap"></param>
            /// <returns>The Fbx channel name or null if there was no match in the list</returns>
            public string GetFbxChannel(string uniChannel)
            {
                return GetFbxValue(uniChannel, MapUnityChannelToFbxChannel);
            }
        }

        // =========== Property Maps ================
        // These are lists that map a Unity property name to it's corresponding Fbx property name.
        // Split up into multiple lists as some are channel and object dependant.

        /// <summary>
        /// Map of Unity transform properties to their FBX equivalent.
        /// </summary>
        private static List<(string, string)> MapTransformPropToFbxProp = new List<(string, string)>()
        {
            ("m_LocalScale", "Lcl Scaling"),
            ("Motion S", "Lcl Scaling"),
            ("m_LocalPosition", "Lcl Translation"),
            ("Motion T", "Lcl Translation"),
            ("m_TranslationOffset", "Translation"),
            ("m_ScaleOffset", "Scaling"),
            ("m_RotationOffset", "Rotation"),
            ("localEulerAnglesRaw", "Lcl Rotation")
        };

        /// <summary>
        /// Map of Unity Aim constraint properties to their FBX equivalent.
        /// </summary>
        private static List<(string, string)> MapAimConstraintPropToFbxProp = new List<(string, string)>()
        {
            ("m_AimVector", "AimVector"),
            ("m_UpVector", "UpVector"),
            ("m_WorldUpVector", "WorldUpVector"),
            ("m_RotationOffset", "RotationOffset")
        };

        /// <summary>
        /// Map of Unity color properties to their FBX equivalent.
        /// </summary>
        private static List<(string, string)> MapColorPropToFbxProp = new List<(string, string)>()
        {
            ("m_Color", "Color")
        };

        /// <summary>
        /// Map of Unity properties to their FBX equivalent.
        /// </summary>
        private static List<(string, string)> MapPropToFbxProp = new List<(string, string)>()
        {
            ("m_Intensity", "Intensity"),
            ("field of view", "FieldOfView"),
            ("m_Weight", "Weight"),
            ("m_FocalLength", "FocalLength"),
            ("m_LensShift.x", "FilmOffsetX"),
            ("m_LensShift.y", "FilmOffsetY")
        };

        /// <summary>
        /// Map of Unity constraint source property name as a regular expression to the FBX property as a string format.
        /// This is necessary because the Unity property contains an index in to an array, and the FBX property contains
        /// the name of the source object.
        /// </summary>
        private static List<(string, string)> MapConstraintSourcePropToFbxProp = new List<(string, string)>()
        {
            (@"m_Sources\.Array\.data\[(\d+)\]\.weight", "{0}.Weight")
        };

        /// <summary>
        /// Map of Unity constraint source transform property name as a regular expression to the FBX property as a string format.
        /// This is necessary because the Unity property contains an index in to an array, and the FBX property contains
        /// the name of the source object.
        /// </summary>
        private static List<(string, string)> MapConstraintSourceTransformPropToFbxProp = new List<(string, string)>()
        {
            (@"m_TranslationOffsets\.Array\.data\[(\d+)\]", "{0}.Offset T"),
            (@"m_RotationOffsets\.Array\.data\[(\d+)\]", "{0}.Offset R")
        };

        /// <summary>
        /// Map of Unity blendshape property name as a regular expression to the FBX property.
        /// This is necessary because the Unity property contains the name of the target object.
        /// </summary>
        private static List<(string, string)> MapBlendshapesPropToFbxProp = new List<(string, string)>()
        {
            (@"blendShape\.(\S+)", "DeformPercent")
        };

        // ================== Channel Maps ======================

        /// <summary>
        /// Map of Unity transform channels to their FBX equivalent.
        /// </summary>
        private static List<(string, string)> MapTransformChannelToFbxChannel = new List<(string, string)>()
        {
            ("x", Globals.FBXSDK_CURVENODE_COMPONENT_X),
            ("y", Globals.FBXSDK_CURVENODE_COMPONENT_Y),
            ("z", Globals.FBXSDK_CURVENODE_COMPONENT_Z)
        };

        /// <summary>
        /// Map of Unity color channels to their FBX equivalent.
        /// </summary>
        private static List<(string, string)> MapColorChannelToFbxChannel = new List<(string, string)>()
        {
            ("b", Globals.FBXSDK_CURVENODE_COLOR_BLUE),
            ("g", Globals.FBXSDK_CURVENODE_COLOR_GREEN),
            ("r", Globals.FBXSDK_CURVENODE_COLOR_RED)
        };

        // =======================================================

        private static PropertyChannelMap TransformPropertyMap = new PropertyChannelMap(MapTransformPropToFbxProp, MapTransformChannelToFbxChannel);
        private static PropertyChannelMap AimConstraintPropertyMap = new PropertyChannelMap(MapAimConstraintPropToFbxProp, MapTransformChannelToFbxChannel);
        private static PropertyChannelMap ColorPropertyMap = new PropertyChannelMap(MapColorPropToFbxProp, MapColorChannelToFbxChannel);
        private static PropertyChannelMap ConstraintSourcePropertyMap = new PropertyChannelMap(MapConstraintSourcePropToFbxProp, null);
        private static PropertyChannelMap ConstraintSourceTransformPropertyMap = new PropertyChannelMap(MapConstraintSourceTransformPropToFbxProp, MapTransformChannelToFbxChannel);
        private static PropertyChannelMap BlendshapeMap = new PropertyChannelMap(MapBlendshapesPropToFbxProp, null);

        private static PropertyChannelMap OtherPropertyMap = new PropertyChannelMap(MapPropToFbxProp, null);

        /// <summary>
        /// Separates and returns the property and channel from the full Unity property name.
        ///
        /// Takes what is after the last period as the channel.
        /// In order to use this have to be certain that there are channels, as there are cases where what is after
        /// the last period is still the property name. E.g. m_Sources.Array.data[0].weight has no channel.
        /// </summary>
        /// <param name="fullPropertyName"></param>
        /// <returns></returns>
        private static UnityPropertyChannelPair GetUnityPropertyChannelPair(string fullPropertyName)
        {
            int index = fullPropertyName.LastIndexOf('.');
            if (index < 0)
            {
                return new UnityPropertyChannelPair(fullPropertyName, null);
            }

            var property = fullPropertyName.Substring(0, index);
            var channel = fullPropertyName.Substring(index + 1);
            return new UnityPropertyChannelPair(property, channel);
        }

        /// <summary>
        /// Get the Fbx property name for the given Unity constraint source property name from the given list.
        ///
        /// This is different from GetFbxProperty() because the Unity constraint source properties contain indices, and
        /// the Fbx constraint source property contains the name of the source object.
        /// </summary>
        /// <param name="uniProperty"></param>
        /// <param name="constraint"></param>
        /// <param name="propertyMap"></param>
        /// <returns>The Fbx property name or null if there was no match in the list</returns>
        private static string GetFbxConstraintSourceProperty(string uniProperty, FbxConstraint constraint, List<(string, string)> propertyMap)
        {
            foreach (var prop in propertyMap)
            {
                var match = System.Text.RegularExpressions.Regex.Match(uniProperty, prop.Item1);
                if (match.Success && match.Groups.Count > 0)
                {
                    var matchedStr = match.Groups[1].Value;
                    int index;
                    if (!int.TryParse(matchedStr, out index))
                    {
                        continue;
                    }
                    var source = constraint.GetConstraintSource(index);
                    return string.Format(prop.Item2, source.GetName());
                }
            }
            return null;
        }

        /// <summary>
        /// Get the Fbx property name for the given Unity blendshape property name from the given list.
        ///
        /// This is different from GetFbxProperty() because the Unity blendshape properties contain the name
        /// of the target object.
        /// </summary>
        /// <param name="uniProperty"></param>
        /// <param name="propertyMap"></param>
        /// <returns>The Fbx property name or null if there was no match in the list</returns>
        private static string GetFbxBlendshapeProperty(string uniProperty, List<(string, string)> propertyMap)
        {
            foreach (var prop in propertyMap)
            {
                var match = System.Text.RegularExpressions.Regex.Match(uniProperty, prop.Item1);
                if (match.Success)
                {
                    return prop.Item2;
                }
            }
            return null;
        }

        /// <summary>
        /// Try to get the property channel pairs for the given Unity property from the given property channel mapping.
        /// </summary>
        /// <param name="uniPropertyName"></param>
        /// <param name="propertyChannelMap"></param>
        /// <param name="constraint"></param>
        /// <returns>The property channel pairs or null if there was no match</returns>
        private static FbxPropertyChannelPair[] GetChannelPairs(string uniPropertyName, PropertyChannelMap propertyChannelMap, FbxConstraint constraint = null)
        {
            // Unity property name is of the format "property.channel" or "property". Handle both cases.
            var possibleUniPropChannelPairs = new List<UnityPropertyChannelPair>();

            // could give same result as already in the list, avoid checking this case twice
            var propChannelPair = GetUnityPropertyChannelPair(uniPropertyName);
            possibleUniPropChannelPairs.Add(propChannelPair);
            if (propChannelPair.property != uniPropertyName)
            {
                possibleUniPropChannelPairs.Add(new UnityPropertyChannelPair(uniPropertyName, null));
            }

            foreach (var uniPropChannelPair in possibleUniPropChannelPairs)
            {
                // try to match property
                var fbxProperty = propertyChannelMap.GetFbxProperty(uniPropChannelPair.property);
                if (string.IsNullOrEmpty(fbxProperty))
                {
                    if (constraint != null)
                    {
                        // check if it's a constraint source property
                        fbxProperty = GetFbxConstraintSourceProperty(uniPropChannelPair.property, constraint, propertyChannelMap.MapUnityPropToFbxProp);
                    }
                    else
                    {
                        // check if it's a blendshape property
                        fbxProperty = GetFbxBlendshapeProperty(uniPropChannelPair.property, propertyChannelMap.MapUnityPropToFbxProp);
                    }
                }

                if (string.IsNullOrEmpty(fbxProperty))
                {
                    continue;
                }

                // matched property, now try to match channel
                string fbxChannel = null;
                if (!string.IsNullOrEmpty(uniPropChannelPair.channel) && propertyChannelMap.MapUnityChannelToFbxChannel != null)
                {
                    fbxChannel = propertyChannelMap.GetFbxChannel(uniPropChannelPair.channel);
                    if (string.IsNullOrEmpty(fbxChannel))
                    {
                        // couldn't match the Unity channel to the fbx channel
                        continue;
                    }
                }
                return new FbxPropertyChannelPair[] { new FbxPropertyChannelPair(fbxProperty, fbxChannel) };
            }
            return null;
        }

        /// <summary>
        /// Map a Unity property name to the corresponding FBX property and
        /// channel names.
        /// </summary>
        public static bool TryGetValue(string uniPropertyName, out FbxPropertyChannelPair[] prop, FbxConstraint constraint = null)
        {
            prop = new FbxPropertyChannelPair[] {};

            // spot angle is a special case as it returns two channel pairs instead of one
            System.StringComparison ct = System.StringComparison.CurrentCulture;
            if (uniPropertyName.StartsWith("m_SpotAngle", ct))
            {
                prop = new FbxPropertyChannelPair[]
                {
                    new FbxPropertyChannelPair("OuterAngle", null),
                    new FbxPropertyChannelPair("InnerAngle", null)
                };
                return true;
            }

            var propertyMaps = new List<PropertyChannelMap>();

            // Try get constraint specific channel pairs first as we know this is a constraint
            if (constraint != null)
            {
                // Aim constraint shares the RotationOffset property with RotationConstraint, so make sure that the correct FBX property is returned
                if (constraint.GetConstraintType() == FbxConstraint.EType.eAim)
                {
                    propertyMaps.Add(AimConstraintPropertyMap);
                }

                propertyMaps.Add(ConstraintSourcePropertyMap);
                propertyMaps.Add(ConstraintSourceTransformPropertyMap);
            }

            // Check if this is a transform, color, or other property and return the channel pairs if they match.
            propertyMaps.Add(TransformPropertyMap);
            propertyMaps.Add(ColorPropertyMap);
            propertyMaps.Add(OtherPropertyMap);
            propertyMaps.Add(BlendshapeMap);

            foreach (var propMap in propertyMaps)
            {
                prop = GetChannelPairs(uniPropertyName, propMap, constraint);
                if (prop != null)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

using Unity.FbxSdk;
using System.Collections.Generic;

namespace FbxExporters
{
    namespace Editor
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
            /// Contains the two dictionaries that map Unity property to FBX property and Unity channel to Fbx channel
            /// for a set of properties.
            /// </summary>
            struct PropertyChannelMap
            {
                public Dictionary<string, string> MapUnityPropToFbxProp;
                public Dictionary<string, string> MapUnityChannelToFbxChannel;

                public PropertyChannelMap(Dictionary<string,string> propertyMap, Dictionary<string, string> channelMap)
                {
                    MapUnityPropToFbxProp = propertyMap;
                    MapUnityChannelToFbxChannel = channelMap;
                }
            }

            // =========== Property Maps ================
            // These are dictionaries that map a Unity property name to it's corresponding Fbx property name.
            // Split up into multiple dictionaries as some are channel and object dependant.

            /// <summary>
            /// Map of Unity transform properties to their FBX equivalent.
            /// </summary>
            private static Dictionary<string, string> MapTransformPropToFbxProp = new Dictionary<string, string>()
                {
                    { "m_LocalScale", "Lcl Scaling" },
                    { "Motion S", "Lcl Scaling" },
                    { "m_LocalPosition", "Lcl Translation" },
                    { "Motion T", "Lcl Translation" },
                    { "m_TranslationOffset", "Translation" },
                    { "m_ScaleOffset", "Scaling" },
                    { "m_RotationOffset", "Rotation" }
                };

            /// <summary>
            /// Map of Unity Aim constraint properties to their FBX equivalent.
            /// </summary>
            private static Dictionary<string, string> MapAimConstraintPropToFbxProp = new Dictionary<string, string>()
                {
                    { "m_AimVector", "AimVector" },
                    { "m_UpVector", "UpVector" },
                    { "m_WorldUpVector", "WorldUpVector" },
                    { "m_RotationOffset", "RotationOffset" }
                };

            /// <summary>
            /// Map of Unity color properties to their FBX equivalent.
            /// </summary>
            private static Dictionary<string, string> MapColorPropToFbxProp = new Dictionary<string, string>()
                {
                    { "m_Color", "Color" }
                };

            /// <summary>
            /// Map of Unity properties to their FBX equivalent.
            /// </summary>
            private static Dictionary<string, string> MapPropToFbxProp = new Dictionary<string, string>()
                {
                    { "m_Intensity", "Intensity" },
                    { "field of view", "FieldOfView" },
                    { "m_Weight", "Weight" }
                };

            /// <summary>
            /// Map of Unity constraint source property name as a regular expression to the FBX property as a string format.
            /// This is necessary because the Unity property contains an index in to an array, and the FBX property contains
            /// the name of the source object.
            /// </summary>
            private static Dictionary<string, string> MapConstraintSourcePropToFbxProp = new Dictionary<string, string>()
                {
                    { @"m_Sources\.Array\.data\[(\d+)\]\.weight", "{0}.Weight" }
                };

            /// <summary>
            /// Map of Unity constraint source transform property name as a regular expression to the FBX property as a string format.
            /// This is necessary because the Unity property contains an index in to an array, and the FBX property contains
            /// the name of the source object.
            /// </summary>
            private static Dictionary<string, string> MapConstraintSourceTransformPropToFbxProp = new Dictionary<string, string>()
                {
                    { @"m_TranslationOffsets\.Array\.data\[(\d+)\]", "{0}.Offset T" },
                    { @"m_RotationOffsets\.Array\.data\[(\d+)\]", "{0}.Offset R" }
                };

            // ================== Channel Maps ======================

            /// <summary>
            /// Map of Unity transform channels to their FBX equivalent.
            /// </summary>
            private static Dictionary<string, string> MapTransformChannelToFbxChannel = new Dictionary<string, string>()
                {
                    { "x", Globals.FBXSDK_CURVENODE_COMPONENT_X },
                    { "y", Globals.FBXSDK_CURVENODE_COMPONENT_Y },
                    { "z", Globals.FBXSDK_CURVENODE_COMPONENT_Z }
                };

            /// <summary>
            /// Map of Unity color channels to their FBX equivalent.
            /// </summary>
            private static Dictionary<string, string> MapColorChannelToFbxChannel = new Dictionary<string, string>()
                {
                    { "b", Globals.FBXSDK_CURVENODE_COLOR_BLUE },
                    { "g", Globals.FBXSDK_CURVENODE_COLOR_GREEN },
                    { "r", Globals.FBXSDK_CURVENODE_COLOR_RED }
                };

            // =======================================================

            private static PropertyChannelMap TransformPropertyMap = new PropertyChannelMap(MapTransformPropToFbxProp, MapTransformChannelToFbxChannel);
            private static PropertyChannelMap AimConstraintPropertyMap = new PropertyChannelMap(MapAimConstraintPropToFbxProp, MapTransformChannelToFbxChannel);
            private static PropertyChannelMap ColorPropertyMap = new PropertyChannelMap(MapColorPropToFbxProp, MapColorChannelToFbxChannel);
            private static PropertyChannelMap ConstraintSourcePropertyMap = new PropertyChannelMap(MapConstraintSourcePropToFbxProp, null);
            private static PropertyChannelMap ConstraintSourceTransformPropertyMap = new PropertyChannelMap(MapConstraintSourceTransformPropToFbxProp, MapTransformChannelToFbxChannel);
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

            private static FbxPropertyChannelPair[] GetChannelPairs(string uniPropertyName, PropertyChannelMap propertyChannelMap)
            {
                // Unity property name is of the format "property.channel". Split by the last '.' and search for the property in the property dict, and channel in the channel dict.
                // If the property name is just "property" then the channel is null.

                var properties = propertyChannelMap.MapUnityPropToFbxProp;
                var channels = propertyChannelMap.MapUnityChannelToFbxChannel;

                // First handle case where there's no channels.
                if (channels == null)
                {
                    string fbxProperty;
                    if (properties.TryGetValue(uniPropertyName, out fbxProperty))
                    {
                        return new FbxPropertyChannelPair[] { new FbxPropertyChannelPair(fbxProperty, null) };
                    }
                    return null;
                }

                var uniPropChannelPair = GetUnityPropertyChannelPair(uniPropertyName);
                if(uniPropChannelPair.channel == null)
                {
                    // We've already checked the case where there are no channels
                    return null;
                }

                var property = uniPropChannelPair.property;
                var channel = uniPropChannelPair.channel;

                string fbxProp;
                if(!properties.TryGetValue(property, out fbxProp))
                {
                    return null;
                }

                string fbxChannel;
                if(!channels.TryGetValue(channel, out fbxChannel))
                {
                    return null;
                }

                return new FbxPropertyChannelPair[] { new FbxPropertyChannelPair(fbxProp, fbxChannel) };
            }

            private static FbxPropertyChannelPair[] GetConstraintSourceChannelPairs(string uniPropertyName, FbxConstraint constraint, PropertyChannelMap propertyChannelMap)
            {
                var properties = propertyChannelMap.MapUnityPropToFbxProp;
                var channels = propertyChannelMap.MapUnityChannelToFbxChannel;

                foreach(var prop in properties)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(uniPropertyName, prop.Key);
                    if (match.Success && match.Groups.Count > 0)
                    {
                        var matchedStr = match.Groups[1].Value;
                        int index;
                        if (!int.TryParse(matchedStr, out index))
                        {
                            continue;
                        }
                        var source = constraint.GetConstraintSource(index);
                        var fbxName = string.Format(prop.Value, source.GetName());

                        // Have the fbx name, now need the channel
                        if(channels == null)
                        {
                            // no channel, we have what we need
                            return new FbxPropertyChannelPair[] { new FbxPropertyChannelPair(fbxName, null) };
                        }

                        var uniPropChannelPair = GetUnityPropertyChannelPair(uniPropertyName);
                        if (uniPropChannelPair.channel == null)
                        {
                            // We've already checked the case where there are no channels
                            return null;
                        }
                        
                        var channel = uniPropChannelPair.channel;
                        string fbxChannel;
                        if (!channels.TryGetValue(channel, out fbxChannel))
                        {
                            return null;
                        }
                        return new FbxPropertyChannelPair[] { new FbxPropertyChannelPair(fbxName, fbxChannel) };
                    }
                }

                return null;
            }

            /// <summary>
            /// Map a Unity property name to the corresponding FBX property and
            /// channel names.
            /// </summary>
            public static bool TryGetValue(string uniPropertyName, out FbxPropertyChannelPair[] prop, FbxConstraint constraint = null)
            {
                prop = new FbxPropertyChannelPair[] { };
                
                // spot angle is a special case as it returns two channel pairs instead of one
                System.StringComparison ct = System.StringComparison.CurrentCulture;
                if (uniPropertyName.StartsWith("m_SpotAngle", ct))
                {
                    prop = new FbxPropertyChannelPair[]{
                            new FbxPropertyChannelPair ("OuterAngle", null),
                            new FbxPropertyChannelPair ("InnerAngle", null)
                        };
                    return true;
                }

                // Try get constraint specific channel pairs first as we know this is a constraint
                if (constraint != null)
                {
                    // Aim constraint shares the RotationOffset property with RotationConstraint, so make sure that the correct FBX property is returned
                    if (constraint.GetConstraintType() == FbxConstraint.EType.eAim)
                    {
                        prop = GetChannelPairs(uniPropertyName, AimConstraintPropertyMap);
                        if (prop != null)
                        {
                            return true;
                        }
                    }

                    var constraintPropertyMaps = new List<PropertyChannelMap>()
                    {
                        ConstraintSourcePropertyMap,
                        ConstraintSourceTransformPropertyMap
                    };

                    foreach(var propMap in constraintPropertyMaps)
                    {
                        prop = GetConstraintSourceChannelPairs(uniPropertyName, constraint, propMap);
                        if(prop != null)
                        {
                            return true;
                        }
                    }
                }

                // Check if this is a transform, color, or other property and return the channel pairs if they match.
                var propertyMaps = new List<PropertyChannelMap>()
                {
                    TransformPropertyMap,
                    ColorPropertyMap,
                    OtherPropertyMap
                };

                foreach (var propMap in propertyMaps)
                {
                    prop = GetChannelPairs(uniPropertyName, propMap);
                    if (prop != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

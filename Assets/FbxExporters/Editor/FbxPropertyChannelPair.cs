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

            /// <summary>
            /// Map of Unity transform properties to their FBX equivalent.
            /// </summary>
            private static Dictionary<string, string> TransformProperties = new Dictionary<string, string>()
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
            private static Dictionary<string, string> AimConstraintProperties = new Dictionary<string, string>()
                {
                    { "m_AimVector", "AimVector" },
                    { "m_UpVector", "UpVector" },
                    { "m_WorldUpVector", "WorldUpVector" },
                    { "m_RotationOffset", "RotationOffset" }
                };

            /// <summary>
            /// Map of Unity transform channels to their FBX equivalent.
            /// </summary>
            private static Dictionary<string, string> TransformChannels = new Dictionary<string, string>()
                {
                    { "x", Globals.FBXSDK_CURVENODE_COMPONENT_X },
                    { "y", Globals.FBXSDK_CURVENODE_COMPONENT_Y },
                    { "z", Globals.FBXSDK_CURVENODE_COMPONENT_Z }
                };

            /// <summary>
            /// Map of Unity color properties to their FBX equivalent.
            /// </summary>
            private static Dictionary<string, string> ColorProperties = new Dictionary<string, string>()
                {
                    { "m_Color", "Color" }
                };

            /// <summary>
            /// Map of Unity color channels to their FBX equivalent.
            /// </summary>
            private static Dictionary<string, string> ColorChannels = new Dictionary<string, string>()
                {
                    { "b", Globals.FBXSDK_CURVENODE_COLOR_BLUE },
                    { "g", Globals.FBXSDK_CURVENODE_COLOR_GREEN },
                    { "r", Globals.FBXSDK_CURVENODE_COLOR_RED }
                };

            /// <summary>
            /// Map of Unity properties to their FBX equivalent.
            /// </summary>
            private static Dictionary<string, string> OtherProperties = new Dictionary<string, string>()
                {
                    { "m_Intensity", "Intensity" },
                    { "field of view", "FieldOfView" },
                    { "m_Weight", "Weight" }
                };

            /// <summary>
            /// Map of empty string to null, used for properties that don't need a channel.
            /// </summary>
            private static Dictionary<string, string> NullChannel = new Dictionary<string, string>() { { "", null } };

            /// <summary>
            /// Map of Unity constraint source property name as a regular expression to the FBX property as a string format.
            /// This is necessary because the Unity property contains an index in to an array, and the FBX property contains
            /// the name of the source object.
            /// </summary>
            private static Dictionary<string, string> ConstraintSourceProperties = new Dictionary<string, string>()
                {
                    { @"m_Sources\.Array\.data\[(\d+)\]\.weight", "{0}.Weight" }
                };

            /// <summary>
            /// Map of Unity constraint source transform property name as a regular expression to the FBX property as a string format.
            /// This is necessary because the Unity property contains an index in to an array, and the FBX property contains
            /// the name of the source object.
            /// </summary>
            private static Dictionary<string, string> ConstraintSourceTransformProperties = new Dictionary<string, string>()
                {
                    { @"m_TranslationOffsets\.Array\.data\[(\d+)\]", "{0}.Offset T" },
                    { @"m_RotationOffsets\.Array\.data\[(\d+)\]", "{0}.Offset R" }
                };

            public FbxPropertyChannelPair(string p, string c) : this()
            {
                Property = p;
                Channel = c;
            }

            private static bool TryGetChannel(string uniPropertyName, string uniName, string propFormat, Dictionary<string, string> channels, out string outChannel)
            {
                outChannel = null;
                foreach (var channel in channels)
                {
                    var uniChannel = channel.Key;
                    var fbxChannel = channel.Value;
                    if (uniPropertyName.EndsWith(string.Format(propFormat, uniName, uniChannel)))
                    {
                        outChannel = fbxChannel;
                        return true;
                    }
                }
                return false;
            }

            private static FbxPropertyChannelPair[] GetChannelPairs(string uniPropertyName, Dictionary<string, string> properties, Dictionary<string, string> channels = null)
            {
                // Unity property name is of the format "property.channel". Split by the last '.' and search for the property in the property dict, and channel in the channel dict.
                // If the property name is just "property" then the channel is null.

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

                int index = uniPropertyName.LastIndexOf('.');
                if (index < 0)
                {
                    return null;
                }

                var property = uniPropertyName.Substring(0, index);
                var channel = uniPropertyName.Substring(index + 1);

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

            private static bool TryGetConstraintSourceChannelPairs(string uniPropertyName, FbxConstraint constraint, Dictionary<string, string> properties, Dictionary<string, string> channels, ref FbxPropertyChannelPair[] channelPairs)
            {
                foreach (var prop in properties)
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
                        string channel;
                        // we've already matched with the property name, just get the channel
                        if (TryGetChannel(uniPropertyName, "", "{1}", channels, out channel))
                        {
                            channelPairs = new FbxPropertyChannelPair[] { new FbxPropertyChannelPair(fbxName, channel) };
                            return true;
                        }
                    }
                }
                return false;
            }

            /// <summary>
            /// Map a Unity property name to the corresponding FBX property and
            /// channel names.
            /// </summary>
            public static bool TryGetValue(string uniPropertyName, out FbxPropertyChannelPair[] prop, FbxConstraint constraint = null)
            {
                System.StringComparison ct = System.StringComparison.CurrentCulture;

                prop = new FbxPropertyChannelPair[] { };
                var propFormat = "{0}.{1}";

                if (constraint != null)
                {
                    // Aim constraint shares the RotationOffset property with RotationConstraint, so make sure that the correct FBX property is returned
                    if (constraint.GetConstraintType() == FbxConstraint.EType.eAim)
                    {
                        prop = GetChannelPairs(uniPropertyName, AimConstraintProperties, TransformChannels);
                        if(prop != null)
                        {
                            return true;
                        }
                    }

                    prop = GetChannelPairs(uniPropertyName, ConstraintSourceProperties);
                    if(prop != null)
                    {
                        return true;
                    }

                    prop = GetChannelPairs(uniPropertyName, ConstraintSourceTransformProperties, TransformChannels);
                    if(prop != null)
                    {
                        return true;
                    }
                }

                // Transform Properties
                if (TryGetChannelPairs(uniPropertyName, propFormat, TransformProperties, TransformChannels, ref prop))
                {
                    return true;
                }

                // Color Properties
                if (TryGetChannelPairs(uniPropertyName, propFormat, ColorProperties, ColorChannels, ref prop))
                {
                    return true;
                }

                // Other Properties
                if (TryGetChannelPairs(uniPropertyName, "{0}", OtherProperties, NullChannel, ref prop))
                {
                    return true;
                }

                // spot angle is a special case as it returns two channel pairs instead of one
                if (uniPropertyName.StartsWith("m_SpotAngle", ct))
                {
                    prop = new FbxPropertyChannelPair[]{
                            new FbxPropertyChannelPair ("OuterAngle", null),
                            new FbxPropertyChannelPair ("InnerAngle", null)
                        };
                    return true;
                }

                return false;
            }
        }
    }
}

/*************************************************************************************
# Copyright 2018 OSIsoft, LLC
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
# <http://www.apache.org/licenses/LICENSE-2.0>
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# NOTE: this script was designed using the v1.0
# version of the OMF specification, as outlined here:
# http://omf-docs.osisoft.com/en/v1.0
# For more info, see OMF Developer Companion Guide:
# http://omf-companion-docs.osisoft.com
****************************************************************************************/

using System;

namespace IngressServiceAPI
{
    /// <summary>
    /// Sample data structure to represent data objects in the target system.
    /// </summary>
    /// 

    class ProviderType
    {
        public string Index { get; set; }
        public string Name { get; set; }

        public const string JsonSchema = @"{
                ""id"": ""ProviderType"",
                ""name"": ""provider type"",
                ""classification"": ""static"",
                ""type"": ""object"",
                ""description"": ""provider type"",
                ""properties"": {
                    ""Index"": {
                        ""type"": ""string"",
                        ""isindex"": true,
                        ""name"": ""not in use"",
                        ""description"": ""not in use""
                    },
                    ""Name"": {
                        ""type"": ""string"",
                        ""isname"": true,
                        ""name"": ""not in use"",
                        ""description"": ""not in use""
                    }
                }
        }";
    }

    class TrackType
    {
        public string Index { get; set; }
        public string Name { get; set; }

        public const string JsonSchema = @"{
                ""id"": ""TrackType"",
                ""name"": ""track type"",
                ""classification"": ""static"",
                ""type"": ""object"",
                ""description"": ""track type"",
                ""properties"": {
                    ""Index"": {
                        ""type"": ""string"",
                        ""isindex"": true,
                        ""name"": ""not in use"",
                        ""description"": ""not in use""
                    },
                    ""Name"": {
                        ""type"": ""string"",
                        ""isname"": true,
                        ""name"": ""not in use"",
                        ""description"": ""not in use""
                    }
                }
        }";

    }

    class StatType
    {
        public DateTime TimeStamp { get; set; }
        public int ItemCount { get; set; }

        public const string JsonSchema =
            @"{
               ""id"": ""StatType"",
                ""name"": ""statistic type"",
                ""classification"": ""dynamic"",
                ""type"": ""object"",
                ""description"": ""not in use"",
                ""properties"": {
                    ""TimeStamp"": {
                        ""format"": ""date-time"",
                        ""type"": ""string"",
                        ""isindex"": true,
                        ""name"": ""not in use"",
                        ""description"": ""not in use""
                    },
                    ""ItemCount"": {
                        ""type"": ""integer"",
                        ""name"": ""Item count"",
                        ""description"": ""PI point data referenced integer attribute""
                    }
                }
            }";
    }

    class FirstStaticType
    {
        public string index { get; set; }
        public string name { get; set; }
        public string StringProperty { get; set; }

        public const string JsonSchema =
            @"{
                ""id"": ""FirstStaticType"",
                ""name"": ""First static type"",
                ""classification"": ""static"",
                ""type"": ""object"",
                ""description"": ""First static asset type"",
                ""properties"": {
                    ""index"": {
                        ""type"": ""string"",
                        ""isindex"": true,
                        ""name"": ""not in use"",
                        ""description"": ""not in use""
                    },
                    ""name"": {
                        ""type"": ""string"",
                        ""isname"": true,
                        ""name"": ""not in use"",
                        ""description"": ""not in use""
                    },
                    ""StringProperty"": {
                        ""type"": ""string"",
                        ""name"": ""First configuration attribute"",
                        ""description"": ""First static asset type's configuration attribute""
                    }
                }
            }";
    }

    class SecondStaticType
    {
        public string index { get; set; }
        public string name { get; set; }
        public string StringProperty { get; set; }

        public const string JsonSchema =
            @"{
                ""id"": ""SecondStaticType"",
                ""name"": ""Second static type"",
                ""classification"": ""static"",
                ""type"": ""object"",
                ""description"": ""Second static asset type"",
                ""properties"": {
                    ""index"": {
                        ""type"": ""string"",
                        ""isindex"": true,
                        ""name"": ""not in use"",
                        ""description"": ""not in use""
                    },
                    ""name"": {
                        ""type"": ""string"",
                        ""isname"": true,
                        ""name"": ""not in use"",
                        ""description"": ""not in use""
                    },
                    ""StringProperty"": {
                        ""type"": ""string"",
                        ""name"": ""Second configuration attribute"",
                        ""description"": ""Second static asset type's configuration attribute""
                    }
                }
            }";
    }


    class FirstDynamicType
    {
        public DateTime timestamp { get; set; }
        public int IntegerProperty { get; set; }

        public const string JsonSchema =
            @"{
               ""id"": ""FirstDynamicType"",
                ""name"": ""First dynamic type"",
                ""classification"": ""dynamic"",
                ""type"": ""object"",
                ""description"": ""not in use"",
                ""properties"": {
                    ""timestamp"": {
                        ""format"": ""date-time"",
                        ""type"": ""string"",
                        ""isindex"": true,
                        ""name"": ""not in use"",
                        ""description"": ""not in use""
                    },
                    ""IntegerProperty"": {
                        ""type"": ""integer"",
                        ""name"": ""Integer attribute"",
                        ""description"": ""PI point data referenced integer attribute""
                    }
                }
            }";
    }

    class SecondDynamicType
    {
        public DateTime timestamp { get; set; }
        public double NumberProperty1 { get; set; }
        public double NumberProperty2 { get; set; }
        public string StringEnum { get; set; }


        public const string JsonSchema =
            @"{
               ""id"": ""SecondDynamicType"",
                ""name"": ""Second dynamic type"",
                ""classification"": ""dynamic"",
                ""type"": ""object"",
                ""description"": ""not in use"",
                ""properties"": {
                    ""timestamp"": {
                        ""format"": ""date-time"",
                        ""type"": ""string"",
                        ""isindex"": true,
                        ""name"": ""not in use"",
                        ""description"": ""not in use""
                    },
                    ""NumberProperty1"": {
                        ""type"": ""number"",
                        ""name"": ""Number attribute 1"",
                        ""description"": ""PI point data referenced number attribute 1"",
                        ""format"": ""float64""
                    },
                    ""NumberProperty2"": {
                        ""type"": ""number"",
                        ""name"": ""Number attribute 2"",
                        ""description"": ""PI point data referenced number attribute 2"",
                        ""format"": ""float64""
                    },
                    ""StringEnum"": {
                        ""type"": ""string"",
                        ""enum"": [""False"", ""True""],
                        ""name"": ""String enumeration"",
                        ""description"": ""String enumeration to replace boolean type""
                    }
                }
            }";
    }


    class ThirdDynamicType
    {
        public DateTime timestamp { get; set; }
        public int IntegerEnum { get; set; }

        public const string JsonSchema =
            @"{
               ""id"": ""ThirdDynamicType"",
                ""name"": ""Third dynamic type"",
                ""classification"": ""dynamic"",
                ""type"": ""object"",
                ""description"": ""not in use"",
                ""properties"": {
                    ""timestamp"": {
                        ""format"": ""date-time"",
                        ""type"": ""string"",
                        ""isindex"": true,
                        ""name"": ""not in use"",
                        ""description"": ""not in use""
                    },
                    ""IntegerEnum"": {
                        ""type"": ""integer"",
                        ""format"": ""int16"",
                        ""name"": ""Integer enumeration"",
                        ""enum"": [0, 1],
                        ""description"": ""Integer enumeration to replace boolean type""
                    }
                }
            }";
    }

}

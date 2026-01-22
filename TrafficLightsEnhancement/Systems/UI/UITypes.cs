using System.Collections;
using System.ComponentModel;
using Colossal.UI.Binding;
using Newtonsoft.Json;

namespace C2VM.TrafficLightsEnhancement.Systems.UI;

public static class UITypes
{
    public struct ItemDivider
    {
        [JsonProperty]
        const string itemType = "divider";
    }

    public struct ItemRadio
    {
        [JsonProperty]
        const string itemType = "radio";

        public string type;

        public bool isChecked;

        public string key;

        public string value;

        public string label;

        public string engineEventName;
    }

    public struct ItemTitle
    {
        [JsonProperty]
        const string itemType = "title";

        public string title;
    }

    public struct ItemMessage
    {
        [JsonProperty]
        const string itemType = "message";

        public string message;
    }

    public struct ItemCheckbox
    {
        [JsonProperty]
        const string itemType = "checkbox";

        public string type;

        public bool isChecked;

        public string key;

        public string value;

        public string label;

        public string engineEventName;
    }

    public struct ItemButton
    {
        [JsonProperty]
        const string itemType = "button";

        public string type;

        public string key;

        public string value;

        public string label;

        public string engineEventName;
    }

    public struct ItemNotification
    {
        [JsonProperty]
        const string itemType = "notification";

        [JsonProperty]
        const string type = "c2vm-tle-panel-notification";

        public string label;

        public string notificationType;

        public string key;

        public string value;

        public string engineEventName;
    }

    public struct ItemRange {
        [JsonProperty]
        const string itemType = "range";

        public string key;

        public string label;

        public float value;

        public string valuePrefix;

        public string valueSuffix;

        public float min;

        public float max;

        public float step;

        public float defaultValue;

        public bool enableTextField;

        public string textFieldRegExp;

        public string engineEventName;
    }

    public struct ItemCustomPhaseHeader
    {
        [JsonProperty]
        const string itemType = "customPhaseHeader";

        public int trafficLightMode;

        public int phaseCount;
    }

    public struct ItemCustomPhase
    {
        [JsonProperty]
        const string itemType = "customPhase";

        public int activeIndex;

        public int activeViewingIndex;

        public int currentSignalGroup;

        public int manualSignalGroup;

        public int index;

        public int length;

        public uint timer;

        public ushort turnsSinceLastRun;

        public ushort lowFlowTimer;

        public float carFlow;

        public ushort carLaneOccupied;

        public ushort publicCarLaneOccupied;

        public ushort trackLaneOccupied;

        public ushort pedestrianLaneOccupied;

        public ushort bicycleLaneOccupied;

        public float weightedWaiting;

        public float targetDuration;

        public int priority;

        public int minimumDuration;

        public int maximumDuration;

        public float targetDurationMultiplier;

        public float intervalExponent;

        public bool linkedWithNextPhase;

        public bool endPhasePrematurely;
        
        
        public int changeMetric;
        public float waitFlowBalance;
        
        
        public int trafficLightMode;
        
        
        public bool carActive;
        public bool publicCarActive;
        public bool trackActive;
        public bool pedestrianActive;
        public bool bicycleActive;
        
        
        public bool hasSignalDelays;
        public int carOpenDelay;
        public int carCloseDelay;
        public int publicCarOpenDelay;
        public int publicCarCloseDelay;
        public int trackOpenDelay;
        public int trackCloseDelay;
        public int pedestrianOpenDelay;
        public int pedestrianCloseDelay;
        public int bicycleOpenDelay;
        public int bicycleCloseDelay;
        
        
        public float carWeight;
        public float publicCarWeight;
        public float trackWeight;
        public float pedestrianWeight;
        public float bicycleWeight;
        public float smoothingFactor;
        
        
        public float flowRatio;
        public float waitRatio;
        
        public bool smartPhaseSelection;
    }

    public struct UpdateCustomPhaseData
    {
        [DefaultValue(-1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int index;

        public string key;

        public object value;
    }

    public struct SetSignalDelayData
    {
        public int edgeIndex;
        public int edgeVersion;
        public int openDelay;
        public int closeDelay;
        public bool isEnabled;
    }

    public struct RemoveSignalDelayData
    {
        public int edgeIndex;
        public int edgeVersion;
    }

    public struct SignalDelayInfo : IJsonWritable
    {
        public int edgeIndex;
        public int edgeVersion;
        public int openDelay;
        public int closeDelay;
        public bool isEnabled;

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(SignalDelayInfo).FullName);
            writer.PropertyName("edgeIndex");
            writer.Write(edgeIndex);
            writer.PropertyName("edgeVersion");
            writer.Write(edgeVersion);
            writer.PropertyName("openDelay");
            writer.Write(openDelay);
            writer.PropertyName("closeDelay");
            writer.Write(closeDelay);
            writer.PropertyName("isEnabled");
            writer.Write(isEnabled);
            writer.TypeEnd();
        }
    }

    public struct WorldPosition : IJsonWritable
    {
        public float x;

        public float y;

        public float z;

        public string key { get => $"{x.ToString("0.0")},{y.ToString("0.0")},{z.ToString("0.0")}"; }

        public static implicit operator WorldPosition(float pos) => new WorldPosition{x = pos, y = pos, z = pos};

        public static implicit operator WorldPosition(Unity.Mathematics.float3 pos) => new WorldPosition{x = pos.x, y = pos.y, z = pos.z};

        public static implicit operator Unity.Mathematics.float3(WorldPosition pos) => new Unity.Mathematics.float3(pos.x, pos.y, pos.z);

        public static implicit operator UnityEngine.Vector3(WorldPosition pos) => new UnityEngine.Vector3(pos.x, pos.y, pos.z);

        public static implicit operator string(WorldPosition pos) => pos.key;

        public override bool Equals(object obj)
        {
            if (obj is not WorldPosition)
            {
                return false;
            }
            return Equals((WorldPosition)obj);
        }

        public bool Equals(WorldPosition other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(WorldPosition).FullName);
            writer.PropertyName("x");
            writer.Write(x);
            writer.PropertyName("y");
            writer.Write(y);
            writer.PropertyName("z");
            writer.Write(z);
            writer.PropertyName("key");
            writer.Write(key);
            writer.TypeEnd();
        }
    }

    public struct ScreenPoint : System.IEquatable<ScreenPoint>, IJsonWritable
    {
        public int top;

        public int left;

        public ScreenPoint(int topPos, int leftPos)
        {
            left = leftPos;
            top = topPos;
        }

        public ScreenPoint(UnityEngine.Vector3 pos, int screenHeight)
        {
            left = (int)pos.x;
            top = (int)(screenHeight - pos.y);
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(ScreenPoint).FullName);
            writer.PropertyName("top");
            writer.Write(top);
            writer.PropertyName("left");
            writer.Write(left);
            writer.TypeEnd();
        }

        public override bool Equals(object obj)
        {
            if (obj is ScreenPoint other){
                return Equals(other);
            }
            return false;
        }

        public bool Equals(ScreenPoint other)
        {
            return other.top == top && other.left == left;
        }

        public override int GetHashCode() => (top, left).GetHashCode();
    }

    public struct ToolTooltipMessage : IJsonWritable
    {
        public string image;

        public string message;

        public ToolTooltipMessage(string image, string message)
        {
            this.image = image;
            this.message = message;
        }

        public void Write(IJsonWriter writer)
        {
            writer.TypeBegin(typeof(ToolTooltipMessage).FullName);
            writer.PropertyName("image");
            writer.Write(image);
            writer.PropertyName("message");
            writer.Write(message);
            writer.TypeEnd();
        }
    }

    public struct ItemTrafficGroup
    {
        [JsonProperty]
        const string itemType = "trafficGroup";

        public int groupIndex;

        public int groupVersion;

        public string name;

        public int memberCount;

        public bool isCoordinated;

        public bool isCurrentJunctionInGroup;

        public bool greenWaveEnabled;

        public float greenWaveSpeed;

        public float greenWaveOffset;
        
        public int leaderIndex;
        
        public int leaderVersion;
        
        public float distanceToLeader;
        
        public int phaseOffset;
        
        public int signalDelay;
        
        public bool isCurrentJunctionLeader;
        
        public int currentJunctionIndex;
        
        public int currentJunctionVersion;
        
        public ArrayList members;
        
        public int previousState;
        
        public float cycleLength;
    }

    public static ItemRadio MainPanelItemPattern(string label, uint pattern, uint selectedPattern)
    {
        return new ItemRadio{label = label, key = "pattern", value = pattern.ToString(), engineEventName = "C2VM.TrafficLightsEnhancement.TRIGGER:CallMainPanelUpdatePattern", isChecked = (selectedPattern & 0xFFFF) == pattern};
    }

    public static ItemCheckbox MainPanelItemOption(string label, uint option, uint selectedPattern)
    {
        return new ItemCheckbox{label = label, key = option.ToString(), value = ((selectedPattern & option) != 0).ToString(), isChecked = (selectedPattern & option) != 0, engineEventName = "C2VM.TrafficLightsEnhancement.TRIGGER:CallMainPanelUpdateOption"};
    }
}
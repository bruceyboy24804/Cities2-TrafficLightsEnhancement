
namespace C2VM.TrafficLightsEnhancement.Extensions
{
    using System;
    using System.Collections.Generic;
    using Colossal.UI.Binding;
    using Game.UI;

    public abstract partial class ExtendedUISystemBase : UISystemBase
    {
        private const string BindingPrefix = "BINDING:";
        private const string TriggerPrefix = "TRIGGER:";

        private readonly List<Action> _updateCallbacks = new();

        protected virtual bool UseKeyPrefixes => true;

        protected virtual bool DefaultAutoUpdate => true;

        private string GetBindingKey(string key)
        {
            return UseKeyPrefixes ? $"{BindingPrefix}{key}" : key;
        }

        private string GetTriggerKey(string key)
        {
            return UseKeyPrefixes ? $"{TriggerPrefix}{key}" : key;
        }

        protected override void OnUpdate()
        {
            foreach (var action in _updateCallbacks)
            {
                action();
            }

            base.OnUpdate();
        }

        public ValueBindingHelper<T> CreateBinding<T>(string key, T initialValue, bool? autoUpdate = null)
        {
            var bindingKey = GetBindingKey(key);
            var shouldAutoUpdate = autoUpdate ?? DefaultAutoUpdate;
            var helper = new ValueBindingHelper<T>(new(Mod.modName, bindingKey, initialValue, new GenericUIWriter<T?>()));

            AddBinding(helper.Binding);

            if (shouldAutoUpdate)
            {
                _updateCallbacks.Add(helper.ForceUpdate);
            }

            return helper;
        }

        public ValueBindingHelper<T> CreateBinding<T>(string key, string setterKey, T initialValue, Action<T>? updateCallBack = null, bool? autoUpdate = null)
        {
            var bindingKey = GetBindingKey(key);
            var triggerKey = GetTriggerKey(setterKey);
            var shouldAutoUpdate = autoUpdate ?? DefaultAutoUpdate;
            var helper = new ValueBindingHelper<T>(new(Mod.modName, bindingKey, initialValue, new GenericUIWriter<T?>()), updateCallBack);
            var trigger = new TriggerBinding<T>(Mod.modName, triggerKey, helper.UpdateCallback, GenericUIReader<T>.Create());

            AddBinding(helper.Binding);
            AddBinding(trigger);

            if (shouldAutoUpdate)
            {
                _updateCallbacks.Add(helper.ForceUpdate);
            }

            return helper;
        }

        public GetterValueBinding<T> CreateBinding<T>(string key, Func<T> getterFunc, bool autoUpdate = true)
        {
            var bindingKey = GetBindingKey(key);
            var binding = new GetterValueBinding<T>(Mod.modName, bindingKey, getterFunc, new GenericUIWriter<T>());

            if (autoUpdate)
            {
                AddUpdateBinding(binding);
            }
            else
            {
                AddBinding(binding);
            }

            return binding;
        }

        public TriggerBinding CreateTrigger(string key, Action action)
        {
            var triggerKey = GetTriggerKey(key);
            var binding = new TriggerBinding(Mod.modName, triggerKey, action);

            AddBinding(binding);

            return binding;
        }

        public TriggerBinding<T1> CreateTrigger<T1>(string key, Action<T1> action)
        {
            var triggerKey = GetTriggerKey(key);
            var binding = new TriggerBinding<T1>(Mod.modName, triggerKey, action, GenericUIReader<T1>.Create());

            AddBinding(binding);

            return binding;
        }

        public TriggerBinding<T1, T2> CreateTrigger<T1, T2>(string key, Action<T1, T2> action)
        {
            var triggerKey = GetTriggerKey(key);
            var binding = new TriggerBinding<T1, T2>(Mod.modName, triggerKey, action, GenericUIReader<T1>.Create(), GenericUIReader<T2>.Create());

            AddBinding(binding);

            return binding;
        }

        public TriggerBinding<T1, T2, T3> CreateTrigger<T1, T2, T3>(string key, Action<T1, T2, T3> action)
        {
            var triggerKey = GetTriggerKey(key);
            var binding = new TriggerBinding<T1, T2, T3>(Mod.modName, triggerKey, action, GenericUIReader<T1>.Create(), GenericUIReader<T2>.Create(), GenericUIReader<T3>.Create());

            AddBinding(binding);

            return binding;
        }

        public TriggerBinding<T1, T2, T3, T4> CreateTrigger<T1, T2, T3, T4>(string key, Action<T1, T2, T3, T4> action)
        {
            var triggerKey = GetTriggerKey(key);
            var binding = new TriggerBinding<T1, T2, T3, T4>(Mod.modName, triggerKey, action, GenericUIReader<T1>.Create(), GenericUIReader<T2>.Create(), GenericUIReader<T3>.Create(), GenericUIReader<T4>.Create());

            AddBinding(binding);

            return binding;
        }
    }
}
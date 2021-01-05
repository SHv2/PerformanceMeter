﻿/*
 * PluginConfig.cs
 * PerformanceMeter
 *
 * This file defines the configuration of PerformanceMeter.
 *
 * This code is licensed under the MIT license.
 * Copyright (c) 2021 JackMacWindows.
 */

using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace PerformanceMeter
{
    public class PluginConfig
    {
        public static PluginConfig Instance { get; set; }
        public virtual bool enabled { get; set; } = true;
        public virtual int mode { get; set; } = (int)MeasurementMode.Energy; // Must be 'virtual' if you want BSIPA to detect a value change and save the config automatically.
        internal MeasurementMode GetMode() { return (MeasurementMode)mode; }

        public enum MeasurementMode {
            Energy,
            PercentModified,
            PercentRaw
        };

        /// <summary>
        /// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
        /// </summary>
        public virtual void OnReload()
        {
            // Do stuff after config is read from disk.
        }

        /// <summary>
        /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        /// </summary>
        public virtual void Changed()
        {
            // Do stuff when the config is changed.
        }

        /// <summary>
        /// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
        /// </summary>
        public virtual void CopyFrom(PluginConfig other)
        {
            // This instance's members populated from other
        }
    }
}

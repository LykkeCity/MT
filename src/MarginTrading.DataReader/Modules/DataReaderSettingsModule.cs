﻿using Autofac;
using MarginSettings = MarginTrading.DataReader.Settings.MarginSettings;

namespace MarginTrading.DataReader.Modules
{
    public class DataReaderSettingsModule : Module
    {
        private readonly MarginSettings _settings;

        public DataReaderSettingsModule(MarginSettings settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings).SingleInstance();
        }
    }
}
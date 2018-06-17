using System;

namespace MarginTrading.Backend.Core
{
    public interface ILogEntity
    {
        DateTime DateTime { get; set; }
        string Level { get; set; }
        string Env { get; set; }
        string AppName { get; set; }
        string Version { get; set; }
        string Component { get; set; }
        string Process { get; set; }
        string Context { get; set; }
        string Type { get; set; }
        string Stack { get; set; }
        string Msg { get; set; }
    }
}
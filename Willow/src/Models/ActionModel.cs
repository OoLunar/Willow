using System;

namespace OoLunar.Willow.Models
{
    public class ActionModel
    {
        public ActionFlags Action { get; init; }
        public Ulid CorrelationId { get; init; }
        public object? Data { get; init; }

        public ActionModel(ActionFlags action, object? data = null)
        {
            Action = action;
            Data = data;
        }
    }

    public enum ActionFlags
    {
        Error,
        Result,
        ExecuteCommand,
        AlterSettings
    }
}

namespace OoLunar.Willow.Models
{
    public sealed class ActionModel
    {
        public ActionFlags Action { get; init; }
        public object? Data { get; init; }

        public ActionModel(ActionFlags action, object? data = null)
        {
            Action = action;
            Data = data;
        }
    }

    public enum ActionFlags
    {
        InvalidAction,
        ExecuteCommand,
        AlterSettings
    }
}

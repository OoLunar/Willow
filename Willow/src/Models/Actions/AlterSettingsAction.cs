namespace OoLunar.Willow.Models.Actions
{
    public class AlterSettingsAction
    {
        public UserModel NewModel { get; init; }

        public AlterSettingsAction(UserModel newModel) => NewModel = newModel;
    }
}

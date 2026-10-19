using UnityEngine.UIElements;

namespace GG.Unity.UserInterface.Buttons.Spinner
{
    [UxmlElement]
    public partial class SpinnerLoadingButton : LoadingButton
    {
        const float DegreesPerMs = 0.36f; // 360 per second
        const string Spinner = "spinner";
        const string ButtonSpinnerClass = "menu-button__spinner";

        readonly VisualElement _spinner;
        readonly IVisualElementScheduledItem _spin;
        float _angle;

        public SpinnerLoadingButton()
        {
            _spinner = new VisualElement { name = Spinner, pickingMode = PickingMode.Ignore };
            _spinner.AddToClassList(ButtonSpinnerClass);
            Add(_spinner);

            _spin = _spinner.schedule.Execute(state =>
            {
                _angle = (_angle + state.deltaTime * DegreesPerMs) % 360f;
                _spinner.style.rotate = new Rotate(new Angle(_angle, AngleUnit.Degree));
            }).Every(16);
            _spin.Pause();
        }

        protected override void OnLoadingChanged(bool loading)
        {
            if (loading) _spin.Resume();
            else _spin.Pause();
        }
    }
}


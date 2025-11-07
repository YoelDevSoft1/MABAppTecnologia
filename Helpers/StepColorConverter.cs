using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MABAppTecnologia.Helpers
{
    public class StepColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int currentStep && parameter is string stepParam && int.TryParse(stepParam, out int stepNumber))
            {
                if (currentStep == stepNumber)
                {
                    // Paso actual: azul brillante
                    return new SolidColorBrush(Color.FromRgb(0, 120, 212));
                }
                else if (currentStep > stepNumber)
                {
                    // Paso completado: verde
                    return new SolidColorBrush(Color.FromRgb(40, 167, 69));
                }
                else
                {
                    // Paso pendiente: gris
                    return new SolidColorBrush(Color.FromRgb(204, 204, 204));
                }
            }

            return new SolidColorBrush(Color.FromRgb(204, 204, 204));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

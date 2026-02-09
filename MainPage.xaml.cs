using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;


namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                string aboutText = await ReadAboutAssetsAsync();
                // Например, отображение текста где-нибудь, например, в ResultLabel
                // ResultLabel.Text = aboutText; // Раскомментируйте при необходимости

            }
            catch (Exception ex)
            {
                // Обработка ошибок чтения файла
                ResultLabel.Text = $"Ошибка чтения файла: {ex.Message}";
            }
        }

        private async Task<string> ReadAboutAssetsAsync()
        {
            var assetName = "Raw\\AboutAssets.txt";
            var stream = await FileSystem.OpenAppPackageFileAsync(assetName);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private bool ValidateAllInputs(out string errorMessage)
        {
            errorMessage = string.Empty;
            var errors = new List<string>();

            // Проверка точки забуривания (Point 1)
            if (string.IsNullOrWhiteSpace(X1Entry.Text))
                errors.Add("X1 - координата X точки забуривания");
            if (string.IsNullOrWhiteSpace(Y1Entry.Text))
                errors.Add("Y1 - координата Y точки забуривания");
            if (string.IsNullOrWhiteSpace(H1Entry.Text))
                errors.Add("H1 - высота точки забуривания");

            // Проверка точки выбуривания (Point 2)
            if (string.IsNullOrWhiteSpace(X2Entry.Text))
                errors.Add("X2 - координата X точки выбуривания");
            if (string.IsNullOrWhiteSpace(Y2Entry.Text))
                errors.Add("Y2 - координата Y точки выбуривания");
            if (string.IsNullOrWhiteSpace(H2Entry.Text))
                errors.Add("H2 - высота точки выбуривания");

            // Проверка точек разворота станка (Point 3 и 4)
            if (string.IsNullOrWhiteSpace(X3Entry.Text))
                errors.Add("X3 - координата X первой точки разворота");
            if (string.IsNullOrWhiteSpace(Y3Entry.Text))
                errors.Add("Y3 - координата Y первой точки разворота");
            if (string.IsNullOrWhiteSpace(X4Entry.Text))
                errors.Add("X4 - координата X второй точки разворота");
            if (string.IsNullOrWhiteSpace(Y4Entry.Text))
                errors.Add("Y4 - координата Y второй точки разворота");

            // Проверка типа станка
            if (MachineTypePicker.SelectedItem == null)
                errors.Add("Тип станка (Robbins или Tumi)");

            // Если есть ошибки, формируем сообщение
            if (errors.Count > 0)
            {
                errorMessage = "Не заполнены следующие поля:\n" + string.Join("\n", errors.Select(e => $"• {e}"));
                return false;
            }

            return true;
        }

        private double ParseDoubleSafely(string input, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException($"Поле {fieldName} пустое");

            // Заменяем запятую на точку для универсальности
            string normalized = input.Replace(',', '.').Trim();

            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            throw new ArgumentException($"Некорректное числовое значение в поле {fieldName}: '{input}'");
        }

        private async void OnCalculateClicked(object sender, EventArgs e)
        {

            if (!ValidateAllInputs(out string validationError))
            {
                ResultLabel.Text = validationError;
                return;
            }

            string aboutText = "";
            try
            {
                aboutText = await ReadAboutAssetsAsync();
                // Можно вывести или сохранить, если нужно
                ResultLabel.Text = aboutText;
            }
            catch (Exception ex)
            {
                ResultLabel.Text = $"!!Ошибка: {ex.Message}";
                return;
            }

            try
            {
                // Чтение данных с формы и расчет
                double x1 = ParseDoubleSafely(X1Entry.Text, "X1");
                double y1 = ParseDoubleSafely(Y1Entry.Text, "Y1");
                double h1 = ParseDoubleSafely(H1Entry.Text, "H1");

                double x2 = ParseDoubleSafely(X2Entry.Text, "X2");
                double y2 = ParseDoubleSafely(Y2Entry.Text, "Y2");
                double h2 = ParseDoubleSafely(H2Entry.Text, "H2");

                double x3 = ParseDoubleSafely(X3Entry.Text, "X3");
                double y3 = ParseDoubleSafely(Y3Entry.Text, "Y3");

                double x4 = ParseDoubleSafely(X4Entry.Text, "X4");
                double y4 = ParseDoubleSafely(Y4Entry.Text, "Y4");

                //double K_deg = string.IsNullOrWhiteSpace(KDegEntry.Text) ? 0 : double.Parse(KDegEntry.Text);
                //double T_deg = string.IsNullOrWhiteSpace(TDegEntry.Text) ? 0 : double.Parse(TDegEntry.Text);

                double K_deg = 0;
                double T_deg = 0;

                string selectedType = MachineTypePicker.SelectedItem as string;

                string result = await CalculateResult(x1, y1, h1, x2, y2, h2, x3, y3, x4, y4, K_deg, T_deg, selectedType);

                ResultLabel.Text = result;
            }
            catch (Exception ex)
            {
                ResultLabel.Text = $"!Ошибка: {ex.Message}";
            }
        }

        private async Task<string> CalculateResult(double x1, double y1, double h1,
                                       double x2, double y2, double h2,
                                       double x3, double y3,
                                       double x4, double y4,
                                       double K_deg, double T_deg, string selectedType)
        {
            var sb = new StringBuilder();

            double RadToDeg(double rad) => rad * 180.0 / Math.PI;
            double DegToRad(double deg) => deg * Math.PI / 180.0;
            string ToDMS(double deg)
            {
                // Пример простого формата D°M'S" (без знаков/наборов)
                double d = Math.Truncate(deg);
                double mFull = Math.Abs((deg - d) * 60.0);
                double m = Math.Truncate(mFull);
                double s = (mFull - m) * 60.0;
                return string.Format("{0}°{1}'{2:0.##}\"", d, m, s);
            }

            // Гориз. вектор от Point1 к Point2
            double dx_h = x2 - x1;
            double dy_h = y2 - y1;
            double d_h = Math.Sqrt(dx_h * dx_h + dy_h * dy_h);

            // Наклонное расстояние
            double dz = h2 - h1;
            double d_nak = Math.Sqrt(dx_h * dx_h + dy_h * dy_h + dz * dz);

            // Вектор направления станка (по Point3->Point4)
            double vx = x4 - x3;
            double vy = y4 - y3;

            // Разворот α = arctan(vx / vy) — используем Atan2(vx, vy)
            double alphaRad = Math.Atan2(vx, vy); // арктангенс(vx/vy) с учётом квадранта
            double alphaDeg = (alphaRad * 180.0 / Math.PI);
            // Если отрицательный — добавить 360°
            if (alphaDeg < 0) { alphaDeg += 360.0; }
            if (alphaDeg > 360) { alphaDeg -= 360.0; }
            var alphaDMS = ToDMS(alphaDeg);

            // Переводим фактический вектор на центр бурения
            double tumiCenter = 0.3518;
            double robbinsCenter = 0.2686;

            // Универсальная функция расчета центра для given угла
            // Возвращает (xCenter, yCenter) для точки (x, y) со смещением radius под углом angleDeg
            // Угол задаётся в градусах. 0° направлен вверх, движение по часовой стрелке.
            (double xCenter, double yCenter) CenterFromAngle(double x, double y, double radius, double angleDeg)
            {
                double a = angleDeg * Math.PI / 180.0;
                // dx и dy соответствуют ориентации: 0° -> смещение по -X, 90° -> +Y и т.д.
                double dx = -radius * Math.Cos(a);
                double dy = radius * Math.Sin(a);
                return (x + dx, y + dy);
            }

            // Объявляем переменные вне блоков if/else
            (double xCenter, double yCenter) c3 = (0, 0);
            (double xCenter, double yCenter) c4 = (0, 0);

            double radius = selectedType == "Robbins" ? robbinsCenter : tumiCenter;

            c3 = CenterFromAngle(x3, y3, radius, alphaDeg);
            c4 = CenterFromAngle(x4, y4, radius, alphaDeg);

            //sb.AppendLine();
            //sb.AppendLine($"x3Center = {c3.xCenter}");
            //sb.AppendLine($"y3Center = {c3.yCenter}");
            //sb.AppendLine($"x4Center = {c4.xCenter}");
            //sb.AppendLine($"y4Center = {c4.yCenter}");

            // Вектор направления станка (по Center)
            double vxCenter = c4.xCenter - c3.xCenter; 
            double vyCenter = c4.yCenter - c3.yCenter;

            // Разворот α = arctan(vx / vy) — используем Atan2(vx, vy)
            double alphaRadCenter = Math.Atan2(vxCenter, vyCenter); // арктангенс(vx/vy) с учётом квадранта
            double alphaDegCenter = (alphaRadCenter * 180.0 / Math.PI);
            // Если отрицательный — добавить 360°
            if (alphaDegCenter < 0) { alphaDegCenter += 360.0; }
            if (alphaDegCenter > 360) { alphaDegCenter -= 360.0; }
            var alphaDMSCenter = ToDMS(alphaDegCenter);

            // Перевод K и T в DMS
            var Kdms = ToDMS(K_deg);

            var Tdms = ToDMS(T_deg);


            // Гориз. проложение по оси T: Lт = dнак * sin T
            double Lt = d_nak * Math.Cos(DegToRad(K_deg)) * Math.Sin(DegToRad(T_deg));


            // Гориз. проложение по оси K: Lк = dнак * sin K
            double Lk = d_nak * Math.Sin(DegToRad(K_deg));


            // Гориз. расстояние от Point1 до фактической точки Point5
            double Dfact = Math.Sqrt(Lt * Lt + Lk * Lk);


            // Угол смещения относительно оси T (Y). Используем Atan2(X, Y) => угол от оси Y.
            double alphaDfactRad = 0.0;
            if (Dfact > 0.0)
                alphaDfactRad = Math.Atan2(Lk, Lt); // первым аргументом X, вторым Y для угла от Y
            double alphaDfactDeg = RadToDeg(alphaDfactRad);
            // нормализуем угол перед показом в DMS
            alphaDfactDeg = ((alphaDfactDeg % 360.0) + 360.0) % 360.0;
            string alphaDfactDMS = ToDMS(alphaDfactDeg);

            // Суммарный угол направления (от оси Y)
            double alphaCombinedRad = alphaRadCenter + alphaDfactRad;
            double alphaCombinedDeg = RadToDeg(alphaCombinedRad);
            alphaCombinedDeg = ((alphaCombinedDeg % 360.0) + 360.0) % 360.0;
            string alphaCombinedDMS = ToDMS(alphaCombinedDeg);

            // Приращения координат в плоскости (X — вправо, Y — вперёд)
            double dX = Dfact * Math.Sin(alphaCombinedRad);
            double dY = -Dfact * Math.Cos(alphaCombinedRad); // <-- поменял знак, чтобы "вперёд" соответствовало уменьшению Y

            double X5 = x1 + dX;
            double Y5 = y1 + dY;
            double H5 = h2; // по условию (высота целевой точки равна h2)

            // Ошибка в горизонтальной плоскости: Point2 - Point5
            double ex = x2 - X5;
            double ey = y2 - Y5;

            // Направление бурения по Point3->Point4: единичный вектор u
            double dxs = vx;
            double dys = vy;
            double Ls = Math.Sqrt(dxs * dxs + dys * dys);
            double ux = dxs / Ls;
            double uy = dys / Ls;

            // Перпендикулярный вектор v = (-uy, ux) (левый поворот)
            double vx_perp = -uy;
            double vy_perp = ux;

            // Проекции ошибки на u (T) и на v (K)
            double projT = ex * ux + ey * uy;      // проекция на ось T
            double projK = ex * vx_perp + ey * vy_perp; // проекция на ось K

            // Вычисляем на сколько необходимо наклонить станок по оси Т относительно проекции ошибки на ось Т

            double alphaTRad = Math.Atan(projT / (h1 - h2));
            //double inclineT = Math.Abs(1500 * Math.Tan(alphaTRad));
            double inclineT = Math.Abs(1500 * Math.Sin(alphaTRad));


            // Вычисляем на сколько необходимо наклонить станок по оси К относительно проекции ошибки на ось К

            double inclineK = 0;

            if (selectedType == "Robbins")
            {
                // действия для Robbins
                double alphaKRad = Math.Atan(projK / (h1 - h2));
                //inclineK = Math.Abs(1210 * Math.Tan(alphaKRad));
                inclineK = Math.Abs(1210 * Math.Sin(alphaKRad));
            }
            else if (selectedType == "Tumi")
            {
                // действия для Tumi
                double alphaKRad = Math.Atan(projK / (h1 - h2));
                //inclineK = Math.Abs(1450 * Math.Tan(alphaKRad));
                inclineK = Math.Abs(1450 * Math.Sin(alphaKRad));
            }
            //double alphaKRad = Math.Atan(projK / (h1 - h2));
            //double inclineK = Math.Abs(1574 * Math.Tan(alphaKRad));

            // Вычисляем куда нужно наклонить по оси Т и К
            string dirT = projT > 0 ? "Назад" : "Вперед";
            string dirK = projK > 0 ? "Поднять левую сторону" : "Поднять правую сторону";


            string result =
                "Входные данные для расчетов:\n" +
                $"Тип станка: {selectedType}\n" +
                "Точка забуривания (X,Y,H):\n" +
                $"{x1}, {y1}, {h1}\n" +
                "Точка выбуривания (X,Y,H):\n" +
                $"{x2}, {y2}, {h2}\n" +
                "Точка разворота станка 1 (X,Y):\n" +
                $"{x3}, {y3}\n" +
                "Точка разворота станка 2 (X,Y):\n" +
                $"{x4}, {y4}\n" +
                "\n" +
                "\n" +
                "\n" +
                "Итоговые наклоны станка:\n" +
                $"По оси T: {dirT} {inclineT:F0} мм\n" +
                $"По оси K: {dirK} {inclineK:F0} мм\n";

            // Переход к новой странице
            await Navigation.PushAsync(new ResultPage(result));


            //sb.AppendLine();
            //sb.AppendLine("Итоговые наклоны:");
            //sb.AppendLine($"По оси T: {dirT} {inclineT:F0} мм");
            //sb.AppendLine($"По оси K: {dirK} {inclineK:F0} мм");

            return sb.ToString();
        }

        //static double ReadDouble(string prompt)
        //{
        //    while (true)
        //    {
        //        Console.Write($"{prompt}: "); string input = Console.ReadLine(); if (string.IsNullOrWhiteSpace(input)) { Console.WriteLine("Пожалуйста, введите число."); continue; }


        //        // Поддержка как запятой, так и точки в качестве разделителя дробной части
        //        string normalized = input.Replace(',', '.');

        //        if (double.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
        //        {
        //            return val;
        //        }

        //        Console.WriteLine("Некорректный ввод. Введите число (например 50.4317).");
        //    }
        //}

        //private double DegToRad(double deg) => deg * Math.PI / 180.0;
        //private double RadToDeg(double rad) => rad * 180.0 / Math.PI;

        //private (int deg, int min, int sec) ToDMS(double degrees)
        //{
        //    int sign = degrees < 0 ? -1 : 1;
        //    double absDeg = Math.Abs(degrees);
        //    int d = (int)Math.Truncate(absDeg);
        //    double rem = (absDeg - d) * 60.0;
        //    int m = (int)Math.Truncate(rem);
        //    double sRem = (rem - m) * 60.0;
        //    int s = (int)Math.Round(sRem);
        //    if (s >= 60)
        //    {
        //        s -= 60; m += 1;
        //    }
        //    if (m >= 60)
        //    {
        //        m -= 60; d += 1;
        //    }
        //    d *= sign;
        //    return (d, m, s);
        //}

        private async void OnQuestionMark1Tapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ImagePage("image1.png"));
        }

        private async void OnQuestionMark2Tapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ImagePage("image2.png"));
        }

        private async void OnQuestionMark3Tapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ImagePage("image3.png"));
        }

        private async void OnQuestionMark4Tapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ImagePage("image4.png"));
        }
    }
}


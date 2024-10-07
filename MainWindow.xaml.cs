using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics; // Для использования Stopwatch

namespace MathPuzzleTrainer
{
    public partial class MainWindow : Window
    {
        private BitmapImage loadedImage;
        private int correctAnswers;
        private int totalQuestions;
        private int currentQuestionIndex;
        private Random random;
        private List<Border> puzzlePieces;
        private Stopwatch stopwatch; // Для отслеживания времени тренировки
        private string selectedTheme = "Addition"; // Значение по умолчанию для темы
                                                   // Переменные для хранения выбранной темы и сложности
        
        private string selectedDifficulty;
        public MainWindow()
        {
            InitializeComponent();
            random = new Random();
            correctAnswers = 0;
            totalQuestions = 16; // Количество вопросов для раскрытия полного пазла
            currentQuestionIndex = 0;
            puzzlePieces = new List<Border>();
            stopwatch = new Stopwatch(); // Инициализируем таймер
        }

        // Обработчик загрузки изображения
        private void AnswerTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Проверяем, была ли нажата клавиша Enter
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                // Вызываем обработчик проверки ответа
                CheckAnswerButton_Click(sender, e);
            }
        }
        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg";

            if (openFileDialog.ShowDialog() == true)
            {
                loadedImage = new BitmapImage(new Uri(openFileDialog.FileName));
                GeneratePuzzleGrid();
                MessageBox.Show("Изображение загружено. Нажмите 'Начать тренировку'.");
            }
        }

        // Обработчик начала тренировки
        private void StartTrainingButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что изображение загружено, тема и сложность выбраны
            if (loadedImage != null && !string.IsNullOrEmpty(selectedTheme) && !string.IsNullOrEmpty(selectedDifficulty))
            {
                // Сброс переменных перед началом тренировки
                correctAnswers = 0;
                currentQuestionIndex = 0;

                // Сброс сетки пазла
                ResetPuzzleGrid();

                // Проверка, есть ли возможность задать новый вопрос
                if (CanAskNewQuestion())
                {
                    // Задаем новый вопрос
                    AskNewQuestion();

                    // Сбрасываем и запускаем таймер
                    stopwatch.Reset();
                    stopwatch.Start();
                }
                else
                {
                    MessageBox.Show("Ошибка: Не удалось задать новый вопрос.");
                }
            }
            else
            {
                // Уведомление о необходимости выбора темы, сложности и загрузки изображения
                if (loadedImage == null)
                {
                    MessageBox.Show("Пожалуйста, сначала загрузите изображение.");
                }
                else if (string.IsNullOrEmpty(selectedTheme))
                {
                    MessageBox.Show("Пожалуйста, выберите тему.");
                }
                else if (string.IsNullOrEmpty(selectedDifficulty))
                {
                    MessageBox.Show("Пожалуйста, выберите уровень сложности.");
                }
            }
        }

        // Генерация сетки пазла
        private void GeneratePuzzleGrid()
        {
            PuzzleGrid.Children.Clear();
            puzzlePieces.Clear();

            int rows = 4; // количество строк
            int columns = 4; // количество столбцов

            // Размеры каждого кусочка изображения
            double pieceWidth = (double)loadedImage.PixelWidth / columns;
            double pieceHeight = (double)loadedImage.PixelHeight / rows;

            // Масштабирование изображения для корректного отображения в сетке
            double scaleX = PuzzleGrid.ActualWidth / loadedImage.PixelWidth;
            double scaleY = PuzzleGrid.ActualHeight / loadedImage.PixelHeight;
            double scale = Math.Min(scaleX, scaleY);

            double scaledPieceWidth = pieceWidth * scale;
            double scaledPieceHeight = pieceHeight * scale;

            // Корректируем размеры самой сетки, чтобы центрировать пазл
            PuzzleGrid.Width = scaledPieceWidth * columns;
            PuzzleGrid.Height = scaledPieceHeight * rows;
            PuzzleGrid.HorizontalAlignment = HorizontalAlignment.Center;
            PuzzleGrid.VerticalAlignment = VerticalAlignment.Center;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    // Определение области изображения для текущего кусочка
                    Int32Rect cropRect = new Int32Rect(
                        (int)(col * pieceWidth),
                        (int)(row * pieceHeight),
                        (int)pieceWidth,
                        (int)pieceHeight
                    );

                    // Создаем часть изображения (кусочек пазла)
                    CroppedBitmap croppedImage = new CroppedBitmap(loadedImage, cropRect);

                    Image pieceImage = new Image
                    {
                        Source = croppedImage,
                        Stretch = Stretch.UniformToFill,
                        Width = scaledPieceWidth,
                        Height = scaledPieceHeight,
                        Visibility = Visibility.Hidden // Скрываем кусочек, пока не решен вопрос
                    };

                    Border puzzlePiece = new Border
                    {
                        BorderBrush = Brushes.Black, // Границы кусочков для видимости сетки
                        BorderThickness = new Thickness(1),
                        Width = scaledPieceWidth,
                        Height = scaledPieceHeight,
                        Child = pieceImage
                    };

                    puzzlePieces.Add(puzzlePiece);
                    PuzzleGrid.Children.Add(puzzlePiece);
                }
            }
        }

        // Сброс пазла перед новой тренировкой
        private void ResetPuzzleGrid()
        {
            foreach (var piece in puzzlePieces)
            {
                piece.Visibility = Visibility.Hidden;
                if (piece.Child is Image img)
                {
                    img.Visibility = Visibility.Hidden;
                }
            }
        }

        // Обработчик проверки ответа
        private void CheckAnswerButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка наличия вопроса
            string questionText = QuestionTextBlock.Text;
            if (string.IsNullOrEmpty(questionText))
            {
                MessageBox.Show("Вопрос отсутствует.");
                return;
            }

            // Разбор вопроса
            string[] parts = questionText.Split(new char[] { ' ', '=', '?' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                MessageBox.Show("Некорректный формат вопроса.");
                return;
            }

            // Извлечение чисел из вопроса
            if (!int.TryParse(parts[0], out int a) || !int.TryParse(parts[2], out int b))
            {
                MessageBox.Show("Некорректные числа в вопросе.");
                return;
            }

            // Проверка ответа в зависимости от темы
            int correctAnswer = 0;
            switch (selectedTheme)
            {
                case "Сложение":
                    correctAnswer = a + b;
                    break;
                case "Вычитание":
                    correctAnswer = a - b;
                    break;
                case "Умножение":
                    correctAnswer = a * b;
                    break;
                case "Деление":
                    if (b == 0)
                    {
                        MessageBox.Show("Деление на ноль невозможно.");
                        return;
                    }
                    correctAnswer = a / b;
                    break;
                default:
                    MessageBox.Show("Тема не поддерживается.");
                    return;
            }

            // Проверка ответа пользователя
            if (int.TryParse(AnswerTextBox.Text, out int userAnswer) && userAnswer == correctAnswer)
            {
                // Ответ правильный
                MessageBox.Show("Молодец!");
                RevealRandomPuzzlePiece();  // Открываем часть пазла
                correctAnswers++;

                // Очистка поля ввода для следующего вопроса
                AnswerTextBox.Text = "";

                // Переход к новому вопросу
                AskNewQuestion();
            }
            else
            {
                // Ответ неверный, даем пользователю попробовать снова
                MessageBox.Show("Ответ неверный. Попробуйте снова.");

                // Очистка поля ввода для новой попытки
                AnswerTextBox.Text = "";
            }
        }
        // Генерация вопроса для сложения
        private void GenerateAdditionQuestion()
        {
            int a, b;

            switch (selectedDifficulty)
            {
                case "Легкая":
                    a = random.Next(1, 20);
                    b = random.Next(1, 20);
                    break;
                case "Средняя":
                    a = random.Next(20, 100);
                    b = random.Next(20, 100);
                    break;
                case "Сложная":
                    a = random.Next(100, 1000);
                    b = random.Next(100, 1000);
                    break;
                default:
                    a = random.Next(1, 20);
                    b = random.Next(1, 20);
                    break;
            }

            QuestionTextBlock.Text = $"{a} + {b} = ?";
        }

        // Генерация вопроса для вычитания
        private void GenerateSubtractionQuestion()
        {
            int a, b;

            switch (selectedDifficulty)
            {
                case "Легкая":
                    a = random.Next(1, 20);
                    b = random.Next(1, a); // Положительный результат
                    break;
                case "Средняя":
                    a = random.Next(20, 100);
                    b = random.Next(1, a);
                    break;
                case "Сложная":
                    a = random.Next(100, 1000);
                    b = random.Next(1, a);
                    break;
                default:
                    a = random.Next(1, 20);
                    b = random.Next(1, a);
                    break;
            }

            QuestionTextBlock.Text = $"{a} - {b} = ?";
        }

        // Генерация вопроса для умножения
        private void GenerateMultiplicationQuestion()
        {
            int a, b;

            switch (selectedDifficulty)
            {
                case "Легкая":
                    a = random.Next(1, 10);
                    b = random.Next(1, 10);
                    break;
                case "Средняя":
                    a = random.Next(10, 20);
                    b = random.Next(10, 20);
                    break;
                case "Сложная":
                    a = random.Next(20, 50);
                    b = random.Next(20, 50);
                    break;
                default:
                    a = random.Next(1, 10);
                    b = random.Next(1, 10);
                    break;
            }

            QuestionTextBlock.Text = $"{a} * {b} = ?";
        }

        // Генерация вопроса для деления
        private void GenerateDivisionQuestion()
        {
            int a, b;

            switch (selectedDifficulty)
            {
                case "Легкая":
                    b = random.Next(1, 10);
                    a = b * random.Next(1, 10); // Делится нацело
                    break;
                case "Средняя":
                    b = random.Next(10, 20);
                    a = b * random.Next(1, 10);
                    break;
                case "Сложная":
                    b = random.Next(20, 50);
                    a = b * random.Next(1, 10);
                    break;
                default:
                    b = random.Next(1, 10);
                    a = b * random.Next(1, 10);
                    break;
            }

            QuestionTextBlock.Text = $"{a} ÷ {b} = ?";
        }

        // Генерация вопроса для десятичных дробей
        private void GenerateDecimalQuestion()
        {
            double a, b;

            switch (selectedDifficulty)
            {
                case "Легкая":
                    a = Math.Round(random.NextDouble() * 10, 2);
                    b = Math.Round(random.NextDouble() * 10, 2);
                    break;
                case "Средняя":
                    a = Math.Round(random.NextDouble() * 100, 2);
                    b = Math.Round(random.NextDouble() * 100, 2);
                    break;
                case "Сложная":
                    a = Math.Round(random.NextDouble() * 1000, 2);
                    b = Math.Round(random.NextDouble() * 1000, 2);
                    break;
                default:
                    a = Math.Round(random.NextDouble() * 10, 2);
                    b = Math.Round(random.NextDouble() * 10, 2);
                    break;
            }

            QuestionTextBlock.Text = $"{a} + {b} = ?";
        }

        // Генерация вопроса для процентов
        private void GeneratePercentageQuestion()
        {
            int a, b;

            switch (selectedDifficulty)
            {
                case "Легкая":
                    a = random.Next(1, 100);
                    b = random.Next(1, 100);
                    break;
                case "Средняя":
                    a = random.Next(100, 500);
                    b = random.Next(1, 100);
                    break;
                case "Сложная":
                    a = random.Next(500, 1000);
                    b = random.Next(1, 100);
                    break;
                default:
                    a = random.Next(1, 100);
                    b = random.Next(1, 100);
                    break;
            }

            QuestionTextBlock.Text = $"{b}% от {a} = ?";
        }

        // Генерация вопроса для линейных уравнений
        private void GenerateLinearEquationQuestion()
        {
            int a, b, c;

            switch (selectedDifficulty)
            {
                case "Легкая":
                    a = random.Next(1, 5);
                    b = random.Next(1, 10);
                    c = random.Next(1, 10);
                    break;
                case "Средняя":
                    a = random.Next(1, 10);
                    b = random.Next(10, 20);
                    c = random.Next(10, 20);
                    break;
                case "Сложная":
                    a = random.Next(1, 20);
                    b = random.Next(20, 50);
                    c = random.Next(20, 50);
                    break;
                default:
                    a = random.Next(1, 5);
                    b = random.Next(1, 10);
                    c = random.Next(1, 10);
                    break;
            }

            QuestionTextBlock.Text = $"{a}x + {b} = {c}. Найдите x.";
        }

        // Генерация вопроса для степеней
        private void GenerateExponentQuestion()
        {
            int a, b;

            switch (selectedDifficulty)
            {
                case "Легкая":
                    a = random.Next(1, 5);
                    b = random.Next(1, 3);
                    break;
                case "Средняя":
                    a = random.Next(5, 10);
                    b = random.Next(2, 4);
                    break;
                case "Сложная":
                    a = random.Next(10, 20);
                    b = random.Next(3, 5);
                    break;
                default:
                    a = random.Next(1, 5);
                    b = random.Next(1, 3);
                    break;
            }

            QuestionTextBlock.Text = $"{a}^{b} = ?";
        }

        // Генерация нового вопроса

        // Открытие случайного кусочка пазла
        private void AskNewQuestion()
        {
            // Проверяем, что есть доступные вопросы
            if (currentQuestionIndex < totalQuestions)
            {
                if (string.IsNullOrEmpty(selectedTheme))
                {
                    MessageBox.Show("Ошибка: Тема не выбрана.");
                    return;
                }

                // Логирование текущей темы для отладки
                Debug.WriteLine($"Текущая тема: {selectedTheme}");

                // Выбор темы и генерация соответствующего вопроса
                switch (selectedTheme)
                {
                    case "Addition":
                        GenerateAdditionQuestion();
                        break;
                    case "Subtraction":
                        GenerateSubtractionQuestion();
                        break;
                    case "Multiplication":
                        GenerateMultiplicationQuestion();
                        break;
                    case "Division":
                        GenerateDivisionQuestion();
                        break;
                    case "Decimals":
                        GenerateDecimalQuestion();
                        break;
                    case "Percentages":
                        GeneratePercentageQuestion();
                        break;
                    case "Linear Equations":
                        GenerateLinearEquationQuestion();
                        break;
                    case "Exponents":
                        GenerateExponentQuestion();
                        break;
                    default:
                        MessageBox.Show("Ошибка: Неизвестная тема.");
                        return;
                }

                // Переход к следующему вопросу
                currentQuestionIndex++;
            }
            else
            {
                // Остановка таймера после завершения всех вопросов
                stopwatch.Stop();

                // Вычисляем точность
                double accuracy = (double)correctAnswers / totalQuestions * 100;

                // Форматируем время в минутах и секундах
                TimeSpan timeTaken = stopwatch.Elapsed;
                string formattedTime = $"{timeTaken.Minutes} мин {timeTaken.Seconds} сек";

                // Показываем результат
                MessageBox.Show($"Вы завершили все вопросы!\n" +
                                $"Точность: {accuracy:F2}%\n" +
                                $"Время: {formattedTime}", "Результаты");

                // Тут можно добавить действия для сброса или завершения тренировки
            }
        }



        // Открытие случайного кусочка пазла
        private void RevealRandomPuzzlePiece()
        {
            List<Border> hiddenPieces = puzzlePieces.FindAll(p => p.Visibility == Visibility.Hidden);
            if (hiddenPieces.Count > 0)
            {
                int index = random.Next(hiddenPieces.Count);
                hiddenPieces[index].Visibility = Visibility.Visible;

                // Опционально: отображение изображения внутри кусочка
                if (hiddenPieces[index].Child is Image img)
                {
                    img.Visibility = Visibility.Visible;
                }
            }
        }
        // Обработчик пункта "О программе"
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Разработчик: Касьян.К.Р\nEmail: karen270688@mail.ru", "О программе");
        }

        // Обработчик выхода из приложения
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Обработчик выбора темы
        // Метод для обработки выбора темы
        private void ThemeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem selectedItem = sender as MenuItem;

            // Проверяем, что элемент не null
            if (selectedItem != null)
            {
                // Снимаем отметки с других тем
                if (selectedItem.Parent is MenuItem parentMenu)
                {
                    foreach (var item in parentMenu.Items)
                    {
                        if (item is MenuItem mi && mi != selectedItem)
                        {
                            mi.IsChecked = false; // Снимаем отметку
                        }
                    }
                }

                // Отмечаем выбранную тему
                selectedItem.IsChecked = true;

                // Присваиваем значение темы, используя либо Tag, либо Header
                selectedTheme = selectedItem.Tag?.ToString() ?? selectedItem.Header.ToString();

                // Обновляем текстовый блок для отображения выбранной темы
                SelectedThemeTextBlock.Text = selectedTheme;
            }
            else
            {
                // Сообщение об ошибке, если не удалось выбрать тему
                MessageBox.Show("Ошибка: Тема не выбрана или не задана.");
            }
        }


        // Метод проверки возможности задать новый вопрос
        private bool CanAskNewQuestion()
        {
            // Пример простой проверки, в зависимости от твоей логики можно расширить
            return !string.IsNullOrEmpty(selectedTheme) && !string.IsNullOrEmpty(selectedDifficulty);
        }



        // Обработчик выбора уровня сложности

        private void DifficultyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem selectedItem = sender as MenuItem;

            // Проверяем, что элемент не null
            if (selectedItem != null)
            {
                // Снимаем отметки с других уровней
                if (selectedItem.Parent is MenuItem parentMenu)
                {
                    foreach (var item in parentMenu.Items)
                    {
                        if (item is MenuItem mi && mi != selectedItem)
                        {
                            mi.IsChecked = false; // Снимаем отметку
                        }
                    }
                }

                // Отмечаем выбранную сложность
                selectedItem.IsChecked = true;

                // Присваиваем значение сложности, используя либо Tag, либо Header
                selectedDifficulty = selectedItem.Tag?.ToString() ?? selectedItem.Header.ToString();

                // Обновляем текстовый блок для отображения выбранной сложности
                SelectedDifficultyTextBlock.Text = selectedDifficulty;
            }
            else
            {
                // Сообщение об ошибке, если не удалось выбрать сложность
                MessageBox.Show("Ошибка: Уровень сложности не выбран или не задан.");
            }
        }



    }
}

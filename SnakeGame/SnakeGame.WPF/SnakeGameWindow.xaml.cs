using SnakeGame.WPF.Entities;
using SnakeGame.WPF.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;
using System.Speech.Synthesis;
namespace SnakeGame.WPF
{
    /// <summary>
    /// Interaction logic for SnakeGameWindow.xaml
    /// </summary>
    public partial class SnakeGameWindow : Window
    {
        const int MaxHighscoreListEntryCount = 5;
        const int SnakeSquareSize = 20;
        const int SnakeStartLength = 3;
        const int SnakeStartSpeed = 400;
        const int SnakeSpeedThreshold = 100;
        private int _currentScore = 0;
        private SpeechSynthesizer _speechSynthesizer = new SpeechSynthesizer();
        private UIElement _snakeFood = null;
        private SolidColorBrush _foodBrush = Brushes.Yellow;
        private DispatcherTimer _gameTickTimer = new DispatcherTimer();
        private Random _rnd = new Random();
        private SolidColorBrush _snakeBodyBrush = Brushes.Red;
        private SolidColorBrush _snakeHeadBrush = Brushes.Violet;
        private List<SnakePart> _snakeParts = new List<SnakePart>();
        private SnakeDirectionEnum _snakeDirection = SnakeDirectionEnum.Right;
        private int _snakeLength = 0;
        public SnakeGameWindow()
        {
            InitializeComponent();
            this._gameTickTimer.Tick += _gameTickTimer_Tick;
            this.LoadHighScoreList();
        }
        private void _gameTickTimer_Tick(object sender, EventArgs e) => this.MoveSnake();
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            this.DrawGameArea();
            this.StartNewGame();
        }
        private void DrawGameArea()
        {
            bool doneDrawingBackground = false;
            int nextX = 0, nextY = 0;
            int rowCounter = 0;
            bool nextIsOdd = false;

            while (doneDrawingBackground is false)
            {
                var rect = new Rectangle
                {
                    Fill = nextIsOdd ? Brushes.CornflowerBlue : Brushes.Green,
                    Width = SnakeSquareSize,
                    Height = SnakeSquareSize
                };
                GameArea.Children.Add(rect);
                Canvas.SetTop(rect, nextY);
                Canvas.SetLeft(rect, nextX);

                nextIsOdd = !nextIsOdd;
                nextX += SnakeSquareSize;
                if (nextX >= GameArea.ActualWidth)
                {
                    nextX = 0;
                    nextY += SnakeSquareSize;
                    rowCounter++;
                    nextIsOdd = rowCounter % 2 != 0;
                }
                if (nextY >= GameArea.ActualHeight)
                {
                    doneDrawingBackground = true;
                }
            }
        }
        /// <summary>
        /// draws the snake
        /// </summary>
        private void DrawSnake()
        {
            foreach (var snakepart in _snakeParts)
            {
                if (snakepart.UIElement is null)
                {
                    snakepart.UIElement = new Rectangle()
                    {
                        Width = SnakeSquareSize,
                        Height = SnakeSquareSize,
                        Fill = (snakepart.Ishead ? this._snakeHeadBrush : this._snakeBodyBrush)
                    };
                    GameArea.Children.Add(snakepart.UIElement);
                    Canvas.SetTop(snakepart.UIElement, snakepart.Position.Y);
                    Canvas.SetLeft(snakepart.UIElement, snakepart.Position.X);
                }
            }
        }
        private void MoveSnake()
        {
            while (this._snakeParts.Count >= this._snakeLength)
            {
                GameArea.Children.Remove(this._snakeParts[0].UIElement);
                this._snakeParts.RemoveAt(0);
            }
            foreach (var snakePart in this._snakeParts)
            {
                (snakePart.UIElement as Rectangle).Fill = this._snakeBodyBrush;
                snakePart.Ishead = false;
            }
            SnakePart snakeHead = this._snakeParts[this._snakeParts.Count - 1];
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;
            switch (this._snakeDirection)
            {
                case SnakeDirectionEnum.Left:
                    nextX -= SnakeSquareSize;
                    break;
                case SnakeDirectionEnum.Right:
                    nextX += SnakeSquareSize;
                    break;
                case SnakeDirectionEnum.Up:
                    nextY -= SnakeSquareSize;
                    break;
                case SnakeDirectionEnum.Down:
                    nextY += SnakeSquareSize;
                    break;
            }
            this._snakeParts.Add(new SnakePart()
            {
                Position = new Point(nextX, nextY),
                Ishead = true
            });
            this.DrawSnake();
            // collision check function
            this.DoCollisionCheck();
        }
        private void StartNewGame()
        {
            this.borderWelcomeMessage.Visibility = Visibility.Collapsed;
            this.borderHighscoreList.Visibility = Visibility.Collapsed;
            this.borderEndOfGame.Visibility = Visibility.Collapsed;

            foreach (var snakeBodyPart in this._snakeParts)
            {
                if (snakeBodyPart.UIElement != null)
                {
                    GameArea.Children.Remove(snakeBodyPart.UIElement);
                }
            }
            this._snakeParts.Clear();
            if (this._snakeFood != null)
            {
                GameArea.Children.Remove(this._snakeFood);
            }
            // reset
            this._currentScore = 0;
            this._snakeLength = SnakeStartLength;
            this._snakeDirection = SnakeDirectionEnum.Right;
            this._snakeParts.Add(new SnakePart()
            {
                Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5)
            });
            this._gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);
            // draw
            this.DrawSnake();
            this.DrawSnakeFood();
            // update status
            this.UpdateGameStatus();
            // enable timer
            this._gameTickTimer.IsEnabled = true;
        }
        private Point GetNextFoodPosition()
        {
            int maxX = (int)(this.GameArea.ActualWidth / SnakeSquareSize);
            int maxY = (int)(this.GameArea.ActualHeight / SnakeSquareSize);
            int foodX = this._rnd.Next(0, maxX) * SnakeSquareSize;
            int foodY = this._rnd.Next(0, maxY) * SnakeSquareSize;

            foreach (var snakePart in this._snakeParts)
            {
                if ((snakePart.Position.X == foodX) && (snakePart.Position.Y == foodY))
                {
                    return GetNextFoodPosition();
                }
            }
            return new Point(foodX, foodY);
        }
        private void DrawSnakeFood()
        {
            var foodPosition = this.GetNextFoodPosition();
            this._snakeFood = new Ellipse()
            {
                Width = SnakeSquareSize,
                Height = SnakeSquareSize,
                Fill = this._foodBrush
            };
            this.GameArea.Children.Add(this._snakeFood);
            Canvas.SetTop(this._snakeFood, foodPosition.Y);
            Canvas.SetLeft(this._snakeFood, foodPosition.X);
        }
        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var originalSnakeDirection = this._snakeDirection;
            switch (e.Key)
            {
                case System.Windows.Input.Key.Up:
                    if (this._snakeDirection != SnakeDirectionEnum.Down)
                    {
                        this._snakeDirection = SnakeDirectionEnum.Up;
                    }
                    break;
                case System.Windows.Input.Key.Down:
                    if (this._snakeDirection != SnakeDirectionEnum.Up)
                    {
                        this._snakeDirection = SnakeDirectionEnum.Down;
                    }
                    break;
                case System.Windows.Input.Key.Right:
                    if (this._snakeDirection != SnakeDirectionEnum.Left)
                    {
                        this._snakeDirection = SnakeDirectionEnum.Right;
                    }
                    break;
                case System.Windows.Input.Key.Left:
                    if (this._snakeDirection != SnakeDirectionEnum.Right)
                    {
                        this._snakeDirection = SnakeDirectionEnum.Left;
                    }
                    break;
                case System.Windows.Input.Key.Space:
                    this.StartNewGame();
                    break;
            }
            if (this._snakeDirection != originalSnakeDirection)
            {
                this.MoveSnake();
            }
        }
        private void DoCollisionCheck()
        {
            var snakeHead = this._snakeParts[this._snakeParts.Count - 1];
            if ((snakeHead.Position.X == Canvas.GetLeft(this._snakeFood)) && snakeHead.Position.Y == Canvas.GetTop(this._snakeFood))
            {
                EatSnakeFood();
                return;
            }
            if ((snakeHead.Position.Y < 0) || (snakeHead.Position.Y >= GameArea.ActualHeight) || (snakeHead.Position.X < 0) || (snakeHead.Position.X >= GameArea.ActualWidth))
            {
                // todo: endgame()
                EndGame();
                return;
            }
            foreach (var snakeBodyPart in this._snakeParts.Take(this._snakeParts.Count - 1))
            {
                if ((snakeHead.Position.X == snakeBodyPart.Position.X) && (snakeHead.Position.Y == snakeBodyPart.Position.Y))
                {
                    // todo: endgame();
                    EndGame();
                }
            }
        }
        private void EndGame()
        {
            bool isNewHighScore = false;
            if (this._currentScore > 0)
            {
                int lowestHighScore = (this.HighScoreList.Count > 0 ? this.HighScoreList.Min(x => x.Score) : 0);
                if ((this._currentScore > lowestHighScore) || (this.HighScoreList.Count < MaxHighscoreListEntryCount))
                {
                    this.borderNewHighscore.Visibility = Visibility.Visible;
                    this.textBoxPlayerName.Focus();
                    isNewHighScore = true;
                }
            }
            if (!isNewHighScore)
            {
                this.textBoxFinalScore.Text = this._currentScore.ToString();
                this.borderEndOfGame.Visibility = Visibility.Visible;
            }
            this._gameTickTimer.IsEnabled = false;
        }
        private void EatSnakeFood()
        {
            this._speechSynthesizer.Speak("yummy");
            this._snakeLength++;
            this._currentScore++;
            int timerInterval = Math.Max(SnakeSpeedThreshold, (int)this._gameTickTimer.Interval.TotalMilliseconds - (this._currentScore * 2));
            this._gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            GameArea.Children.Remove(this._snakeFood);
            DrawSnakeFood();
            UpdateGameStatus();
        }
        private void UpdateGameStatus()
        {
            this.txtBoxStatusScore.Text = this._currentScore.ToString();
            this.txtBoxStatusSpeed.Text = this._gameTickTimer.Interval.TotalMilliseconds.ToString();
        }
        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            // clos the app
            this.Close();
        }
        private void btnShowHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            this.borderWelcomeMessage.Visibility = Visibility.Collapsed;
            this.borderHighscoreList.Visibility = Visibility.Visible;
        }
        private void buttonAddToHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            int newIndex = 0;
            if ((this.HighScoreList.Count > 0) && (this._currentScore < this.HighScoreList.Max(_ => _.Score)))
            {
                var justAbove = this.HighScoreList.OrderByDescending(_ => _.Score).First(_ => _.Score >= this._currentScore);
                if (justAbove != null)
                {
                    newIndex = this.HighScoreList.IndexOf(justAbove) + 1;
                }
                this.HighScoreList.Insert(newIndex, new SnakeHighScore()
                {
                    PlayerName = this.textBoxPlayerName.Text,
                    Score = this._currentScore
                });
                while (this.HighScoreList.Count > MaxHighscoreListEntryCount)
                {
                    this.HighScoreList.RemoveAt(MaxHighscoreListEntryCount);
                }
                this.SaveHighScoreList();
                this.borderNewHighscore.Visibility = Visibility.Collapsed;
                this.borderHighscoreList.Visibility = Visibility.Visible;
            }
        }
        public ObservableCollection<SnakeHighScore> HighScoreList
        {
            get; set;
        } = new ObservableCollection<SnakeHighScore>();
        private void LoadHighScoreList()
        {
            if (File.Exists("snake_highscore-wpf.xml"))
            {
                var xmlSerialiser = new XmlSerializer(typeof(List<SnakeHighScore>));
                using (var streamReader = new FileStream("snake_highscore-wpf.xml", FileMode.Open))
                {
                    var tempList = (List<SnakeHighScore>)xmlSerialiser.Deserialize(streamReader);
                    tempList = tempList.OrderByDescending(_ => _.Score).ToList();
                    this.HighScoreList.Clear();
                    tempList.ForEach(_ => this.HighScoreList.Add(_));
                }
            }
        }
        private void SaveHighScoreList()
        {
            var serializer = new XmlSerializer(typeof(ObservableCollection<SnakeHighScore>));
            using (Stream writer = new FileStream("snake_highscore-wpf.xml", FileMode.Create))
            {
                serializer.Serialize(writer, this.HighScoreList);
            }
        }
    }
}

using System.Windows;
namespace SnakeGame.WPF.Entities
{
    public class SnakePart
    {
        public UIElement UIElement { get; set; }
        public Point Position { get; set; }
        public bool Ishead { get; set; }
    }
}

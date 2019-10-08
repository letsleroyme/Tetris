
using System.Threading;
using System.Windows.Forms;

namespace Tetris
{
    public partial class Form1 : Form
    {

        Tetris t;
        public Form1()
        {
            InitializeComponent();
            t = new Tetris(panel1, panel2, ScoreLabel);
        }
        //слушатель нажатия клавиш
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if ( t != null)
            {
                t.OnKeyDown(e);//передаем событие нажатия клавиши клавиатуры в объект класса Tetris
            }
        }

        private void Form1_Shown(object sender, System.EventArgs e)
        {
            t.Start();//когда форма отобразилась, игру нужно запустить
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}

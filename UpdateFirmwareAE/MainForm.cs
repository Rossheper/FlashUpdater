using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UFA.ProgrammFlash;
using System.Security.Cryptography;
//using TKB995STD1553B;
using UFA.XML;
using System.Reflection;
using System.Collections;
using System.Threading;

namespace UpdateFirmwareAE
{
    public partial class MainForm : Form
    {
        public class MsgStyle
        {
            public Color Color { get; set; }
            public Font Font { get; set; }

            public MsgStyle()
            {
                Color = Color.Green;
                Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Bold);
            }
            public MsgStyle(Color newColor, Font newFont)
            {
                Color = newColor;
                Font = newFont;
            }
        }
        public class GridContainer
        {
            public string NameField
            {
                get; set;
            }
            public string ValueField
            {
                get; set;
            }

            public GridContainer()
            {
                NameField = null;
                ValueField = null;
            }
            public GridContainer(string name, string val)
            {
                NameField = name;
                ValueField = val;
            }
        }
        // Пока не понял как изменить цвет
        public class ProgressBarExt : ProgressBar
        {
            public Brush ProgressBrush { get; set; }
            public ProgressBarExt() : base()
            {
                // Цвет по-умолчанию
                ProgressBrush = Brushes.Green;
                this.SetStyle(ControlStyles.UserPaint, true);
            }
            protected override void OnPaint(PaintEventArgs e)
            {

                base.OnPaint(e);

                e.Graphics.FillRectangle(Brushes.Red, e.ClipRectangle);
            }
        }
        public class Cont
        {
            public ProgrammFlash progFlashObj;
            public TextBox textBox;

            public Cont(ProgrammFlash pp, TextBox tt)
            {
                progFlashObj = pp;
                textBox = tt;
            }
            public static implicit operator TextBox(Cont container)
            {
                return container.textBox;
            }
            public static implicit operator ProgrammFlash(Cont container)
            {
                return container.progFlashObj;
            }
        }

        /// <summary>
        /// Класс ошибок Валидации и состояний
        /// </summary>
        protected class ErrorContainer
        {
            public ErrorProvider errorProvider { get; set; }
            public bool errorState { get; set; }

            public ErrorContainer(ContainerControl obj)
            {
                errorProvider = new ErrorProvider(obj);
                errorState = true;
            }
        }

        private static List<string> MsgStates = new List<string>() { "Ошибка", "Успех", "Выполнение" };
        private static List<string> OperationState = new List<string>() { "Отключение прерываний АЭ", "Очистка флеш-памяти", "Перепрограммирование" };
        // 0                             1                           2

        public enum OperationStep
        {
            InterruptOff = 0,
            ClearFlash = 1,
            Programming = 2,
            Processing = 3
        }
        #region Поля
        // Объекты с информацией о файлах
        private FileInfo _file_FRM, _file_PLIS;
        // Объект CheckBox для прошивки "по-старому" (ПЛИС+DPS)
        private CheckBox _btn_old_version_prog;
        // Объект класса, описывающий программирование ПЛИС и DSP
        private ProgrammFlash _frm;
        // Контейнер для всех элементов TextBox формы и ErrorProviders для отслеживания ошибок
        private Dictionary<TextBox, ErrorContainer> _checkErrors;
        // Переменная с глобальной (общей ошибкой) для старой версии прошивки
        private bool _commonError = false;

        private XMLSettingsParser _loadSettings;

        #endregion
        public MainForm()
        {
            InitializeComponent();
            NewControls();
            openFileDialogFRM.RestoreDirectory = true;
            openFileDialog_PLIS.RestoreDirectory = true;
        }

        #region Создание и инициализация новых объектов управления

        /// <summary>
        /// Создание Кнопки, выполняющей функции программирования ПЛИС и DPS одновременно
        /// </summary>
        private void NewControls()
        {
            _btn_old_version_prog = new CheckBox();
            _btn_old_version_prog.Location = new Point(button_progFRMfile.Location.X, button_progFRMfile.Location.Y + button_progFRMfile.Height);
            _btn_old_version_prog.Size = new Size(new Point(button_progFRMfile.Size.Width, button_progFRMfile.Size.Height));
            _btn_old_version_prog.Visible = true;
            _btn_old_version_prog.Text = "Прошить АЭ";
            _btn_old_version_prog.Name = "btn_old_version_prog";
            _btn_old_version_prog.Enabled = false;
            _btn_old_version_prog.Appearance = Appearance.Button;
            _btn_old_version_prog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            _btn_old_version_prog.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            _btn_old_version_prog.Click += new System.EventHandler(this.btn_old_version_prog_Click);
            _btn_old_version_prog.TabIndex = 3;
        }
        #endregion

        /// <summary>
        /// Кнопка открытия 
        /// </summary>
        /// <param name="sender">Объект, порадивший событие</param>
        /// <param name="e">Аргументы состояния события</param>
        private void button_openFRMfile_Click(object sender, EventArgs e)
        {

            if (openFileDialogFRM.ShowDialog() == DialogResult.OK)
            {
                textBox_nameFRMfile.Text = openFileDialogFRM.SafeFileName;
                _file_FRM = new FileInfo(openFileDialogFRM.FileName);
                toolTip_FRM.SetToolTip(textBox_nameFRMfile, _file_FRM.FullName);
                FRM_CausesValidationChanged(new Cont(_frm, textBox_nameFRMfile), e);
            }
            else
            {
                button_progFRMfile.Enabled = false;
                MessageBox.Show(this, "Не выбран файл с прошивкой", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Кнопка запуска перепрошивки DSP
        /// </summary>
        /// <param name="sender">Объект, порадивший событие</param>
        /// <param name="e">Аргументы состояния события</param>
        private async void button_progFRMfile_click(object sender, EventArgs e)
        {
            // Отжатие кнопки только после успешного программирования!
            if (button_progFRMfile.Checked)
            {
                richTextBoxLog.Clear();
                toolStripProgressBar.Value = 0;
                toolStripProgressBar.Maximum = 4;
                button_progFRMfile.Enabled = false;

                // Начало программирования
                await StartProgramming(_file_FRM, TypeFRM.DSP);

                button_progFRMfile.CheckState = CheckState.Unchecked;
                button_progFRMfile.Enabled = true;
            }
        }

        /// <summary>
        /// Программирование отдельно ПЛИС или TMS
        /// </summary>
        /// <param name="file"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private async Task StartProgramming(FileInfo file, TypeFRM type)
        {
            gridContainerBindingSource1.Clear();

            toolStripProgressBar.Value = 0;
            toolStripProgressBar.Maximum = OperationState.Count;

            ProgrammState result = new ProgrammState(PrgState.MILSTDError); // Тупо дефолт;
            // Предохранитель
            if (OperationState.Count < 3 || OperationState.Count > 3)
            {
                throw new Exception("Неверное количество команд, необходимых для выполнения перепрошивки!");
            }

            for (int i = 0; i < OperationState.Count; i++) // <= так как программирование ПЛИС и DSP (4 операции (от 0 до 3))
            {
                if (i == 0)
                    result = await DisableInterrupt(i, this);   // 0. Отключение прерываний
                else if (i == 1)
                {
                    ushort start = 0;
                    ushort stop = 0;
                    if (type == TypeFRM.DSP)
                    {
                        start = _frm.PagesZone.DSP.Minimum;
                        stop = _frm.PagesZone.DSP.Maximum;
                    }
                    else
                    {
                        start = _frm.PagesZone.PLIS.Minimum;
                        stop = _frm.PagesZone.PLIS.Maximum;
                    }
                    if (start == stop || stop < start)
                        result = new ProgrammState(PrgState.MILSTDError); // Тупо дефолт
                    else
                    {
                        UpdateStepLog(i, OperationStep.ClearFlash, gridContainerBindingSource1, PrgState.Processing, type);              // Выставляю в таблице статус "Выполняется"
                        UpdateStepLog(i, OperationStep.Processing, gridContainerBindingSource1, PrgState.Processing, null, 0, "%"); // Выставляю в таблице статус ход выполнения 0 c
                        for (ushort page = start; page < stop; page++)
                        {
                            result = await EraseFlashByPage(i, page, type);                                                          // 1. Очистка сектора флеш-памяти DSP
                            UpdateStepLog(i, OperationStep.Processing, gridContainerBindingSource1, result.state, null, (int)(((double)page / stop) * 100), "%"); // Выставляю в таблице статус ход выполнения c
                        }
                        if (result.state == PrgState.Finished)
                            UpdateStepLog(i, OperationStep.ClearFlash, gridContainerBindingSource1, result.state, type);                            // Откл. прерываний - Успех
                    }
                }
                else if (i == 2)
                {
                    if (type == TypeFRM.DSP)
                    {
                        result = await ProgrammingDSP(i, this);
                    }
                    else
                    {
                        result = await ProgrammingPLIS(i, this);
                    }
                    //result = await Programming(i, file, type, this);     // 2. Прошивка PLIS или DSP в зависимости от типа
                }

                else
                    result = new ProgrammState(PrgState.MILSTDError); // Тупо дефолт

                if (result.state != PrgState.Finished)
                {
                    if (i == 0)
                        UpdateStepLog(i, (OperationStep)i, gridContainerBindingSource1, result.state); // 0 1
                    else
                    {
                        if (i > 0)
                            UpdateStepLog(i, (OperationStep)i, gridContainerBindingSource1, result.state, type); // 2
                    }
                    toolStripStatusState.Text = "Ошибка при обновлении прошивки!";
                    toolStripStatusState.ForeColor = Color.Red;

                    MessageBox.Show(result.message);
                    break;
                }
                else
                    toolStripProgressBar.Increment(1); // Прогресс бар +1
            }
        }

        private async Task<ProgrammState> EraseFlashByPage(int step, ushort page, TypeFRM typename)
        {
            ProgrammState result;
            int countErrors = 0;

            result = await Task.Run(() =>
            {
                // Обход всех страниц

                result = _frm.FlashCommand(CMD.EraseFlashSector, FORMATS_MILSTD.F1, page);                       // 1. Очистка секторов
                                                                                                                 //do
                                                                                                                 //{
                                                                                                                 //    result = _frm.FlashCommand(CMD.EraseFlashSector, FORMATS_MILSTD.F1, page);                       // 2. Очистка секторов
                                                                                                                 //}
                                                                                                                 //while (result.state != PrgState.Finished);
                if (result.state == PrgState.Finished)
                {
                    do
                    {
                        System.Threading.Thread.Sleep(1000); // Пауза 1 секунда
                        result = _frm.FlashCommand(CMD.EraseFlashSector, FORMATS_MILSTD.F2);

                        countErrors++;
                        if (countErrors > 30) // в течение 30 с пытаюсь очистить флешку
                        {
                            result = new ProgrammState(PrgState.MILSTDError, String.Format("Ошибка при очистки флеш-памяти. Время ожидания отключения составило {0} с. \nПерепрошивка остановлена.", countErrors));
                            break;
                        }
                    }
                    while (result.state != PrgState.Finished);
                }
                return new ProgrammState(result, "Очистка секторов успешна!");

            });
            //if (result.state == PrgState.Finished)
            //    UpdateStepLog(step, OperationStep.ClearFlash, gridContainerBindingSource1, result.state, typename);                            // Откл. прерываний - Успех
            return result;
        }

        /// <summary>
        /// Метод, загружающий форму
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                _loadSettings = new XMLSettingsParser();
            }
            catch (Exception err)
            {
                MessageBox.Show(String.Format("Файл с настройками не был загружен, поврежден или отсутствует!\n{0}", err.Message), "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            ADDR_SUB loadAS = _loadSettings.GetAddrSub;
            int plate = _loadSettings.GetNumberPlate;

            int pageStartDSP = _loadSettings.GetDspStartPage;
            int pageStartPLIS = _loadSettings.GetPLISStartPage;

            _frm = new ProgrammFlash(new ProgrammFlash.FRM_ADDR_SUB((short)loadAS.addr, (short)loadAS.sub), plate);

            _frm.PagesZone.DSP.Minimum = (ushort)pageStartDSP;
            _frm.PagesZone.PLIS.Minimum = (ushort)pageStartPLIS;


            if (_frm.ConnectToMILSTD == false)
            {
                MessageBox.Show("Связь с платой интерфейса MILSTD-1553B производства Элкус не установлена!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }

            if (loadAS.sub != 16)
            {
                checkBox_version.Enabled = false;
            }
            else
            {

                checkBox_version.Checked = true;
            }

            GenErrorProviders();                    // Создание словаря TextBox - ErrorProvider
            CheckVersion(checkBox_version.Checked);

            gridContainerBindingSource.Add(new GridContainer("Номер платы", plate.ToString()));
            gridContainerBindingSource.Add(new GridContainer("Адрес ОУ", loadAS.addr.ToString()));
            gridContainerBindingSource.Add(new GridContainer("Подадрес ОУ", loadAS.sub.ToString()));
            gridContainerBindingSource.Add(new GridContainer("Страница DSP", pageStartDSP.ToString("X")));
            gridContainerBindingSource.Add(new GridContainer("Страница PLIS", pageStartPLIS.ToString("X")));
        }

        /// <summary>
        /// Метод, создающий словарь TextBox - ErrorProviders для каждого элемента TextBox формы
        /// </summary>
        private void GenErrorProviders()
        {
            _checkErrors = new Dictionary<TextBox, ErrorContainer>();

            foreach (var cnt in this.Controls)
                if (cnt is TextBox)
                    _checkErrors.Add((TextBox)cnt, new ErrorContainer(this));
        }

        private void button_openPLISfile_Click(object sender, EventArgs e)
        {
            if (openFileDialog_PLIS.ShowDialog() == DialogResult.OK)
            {
                textBox_namePLISfile.Text = openFileDialog_PLIS.SafeFileName;
                _file_PLIS = new FileInfo(openFileDialog_PLIS.FileName);
                toolTip_PLIS.SetToolTip(textBox_namePLISfile, _file_PLIS.FullName);
                FRM_CausesValidationChanged(new Cont(_frm, textBox_namePLISfile), e);

                // Принудительно вызываю валидацию (в ней происходит проверка файла)
                textBox_namePLISfile.CausesValidation = !textBox_namePLISfile.CausesValidation;
            }
            else
            {
                button_progPLISfile.Enabled = false;
                MessageBox.Show(this, "Не выбран файл с прошивкой", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Метод, выполняющий обновление ПО PLIS и DSP (для двоих сразу) последовательно
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btn_old_version_prog_Click(object sender, EventArgs e)
        {
            toolStripStatusState.Text = "Запущено обновление прошивки АЭ!";
            toolStripStatusState.ForeColor = Color.Black;
            await ProgrammPLISandDSP(sender);
        }

        private async Task ProgrammPLISandDSP(object sender)
        {
            gridContainerBindingSource1.Clear();

            CheckBox currCheck = sender as CheckBox;
            if ((currCheck != null) && (currCheck.Checked))
            {
                //_frm.DefaultFRMAddressing = new ProgrammFlash.FRM_ADDR_SUB(10, 16);

                toolStripProgressBar.Value = 0;
                toolStripProgressBar.Maximum = 4;
                currCheck.Enabled = false;

                ProgrammState result;
                // Предохранитель
                if (OperationState.Count < 3 || OperationState.Count > 3)
                {
                    throw new Exception("Неверное количество команд, необходимых для выполнения перепрошивки!");
                }

                for (int i = 0; i <= OperationState.Count; i++) // <= так как программирование ПЛИС и DSP (4 операции (от 0 до 3))
                {
                    if (i == 0)
                        result = await DisableInterrupt(i, this);   // 0. Отключение прерываний
                    else if (i == 1)
                        result = await EraseFlashAll(i, this);      // 1. Очистка флешку
                    else if (i == 2)
                        result = await ProgrammingPLIS(i, this);    // 2. Прошивка PLIS
                    else if (i == 3)
                        result = await ProgrammingDSP(i, this);     // 3. Прошивка DSP
                    else
                        result = new ProgrammState(PrgState.MILSTDError); // Тупо дефолт

                    if (result.state != PrgState.Finished)
                    {
                        if (i < 2)
                            UpdateStepLog(i, (OperationStep)i, gridContainerBindingSource1, result.state); // 0 1
                        else
                        {
                            if (i == 2)
                                UpdateStepLog(i, (OperationStep)i, gridContainerBindingSource1, result.state, TypeFRM.PLIS); // 2
                            else if (i == 3)
                                UpdateStepLog(i, (OperationStep)(i - 1), gridContainerBindingSource1, result.state, TypeFRM.DSP); // 3
                        }
                        toolStripStatusState.Text = "Ошибка при обновлении прошивки!";
                        toolStripStatusState.ForeColor = Color.Red;

                        MessageBox.Show(result.message);
                        break;
                    }

                    else
                        toolStripProgressBar.Increment(1); // Прогресс бар +1
                }
                currCheck.CheckState = CheckState.Unchecked;
                currCheck.Enabled = true;
            }
        }


        private void UpdateStateProcessing(int indexStep, int? value = 0, string designation = null)
        {
            if (value > 100)
                value = 100;
            dataGridView1.Rows[indexStep].Cells["State"].Value = value.ToString() + " " + designation;
        }

        /// <summary>
        /// Метод отображающий текущее состояние перепрограммирования
        /// </summary>
        /// <param name="indexStep">Номер шага (он же номер строки в таблице)</param>
        /// <param name="operStep">Текущий шаг</param>
        /// <param name="source">Источник данных для DataGridView</param>
        /// <param name="prgstate">Результат выполнения операции (состояние обмена/перепрограммирования)</param>
        /// <param name="type">Тип прошивки (ПЛИС или DPS)</param>
        /// <param name="value">Значение строки прогресса (ТОЛЬКО для operStep = Processing) </param>
        private void UpdateStepLog(int indexStep, OperationStep operStep, BindingSource source, PrgState prgstate, TypeFRM? type = null, int? value = 0, string designation = null)
        {
            if (operStep == OperationStep.Processing)
            {
                UpdateStateProcessing(indexStep, value, designation);
            }
            else
            {
                string operation = NameOperation((int)operStep) + " ";        // Выбираю текущую операцию по индексу операции
                if (type != null)
                    operation += type.ToString();

                int state = MsgState(prgstate);
                string msgstate = MsgStates[state];    // Выбираю состояние (ошибка или успех)

                if (source.Count > 0)
                {
                    if (indexStep != source.Count)
                        source.RemoveAt(indexStep);
                }
                source.Insert(indexStep, new GridContainer(operation, msgstate));

                MsgStyle newStyle;
                if (state == 0)
                    newStyle = new MsgStyle(Color.Red, new Font(FontFamily.GenericSansSerif, 9, FontStyle.Bold));
                else if (state == 1)
                    newStyle = new MsgStyle(Color.Green, new Font(FontFamily.GenericSansSerif, 9, FontStyle.Bold));
                else
                    newStyle = new MsgStyle(Color.Black, new Font(FontFamily.GenericSansSerif, 9, FontStyle.Regular));

                dataGridView1.Rows[indexStep].Cells["State"].Style.ForeColor = newStyle.Color;
                dataGridView1.Rows[indexStep].Cells["State"].Style.Font = newStyle.Font;
            }
        }

        private int MsgState(PrgState prgstate)
        {
            if (prgstate == PrgState.Finished)
            {
                return 1;
            }
            else if (prgstate == PrgState.Processing)
            {
                return 2;
            }
            else
            {
                return 0;
            }
        }
        private string NameOperation(int indexStep)
        {
            return OperationState[indexStep].Trim();
        }

        /// <summary>
        /// Метод, выполняющий перепрограммирование DSP
        /// </summary>
        /// <param name="result">Объект с описанием текущего состояния</param>
        /// <returns></returns>
        private async Task<ProgrammState> ProgrammingDSP(int step, MainForm owner)
        {
            ProgrammState result;
            UpdateStepLog(step, OperationStep.Programming, gridContainerBindingSource1, PrgState.Processing, TypeFRM.DSP);   // Начало прошивки DSP

            result = await Programming(step, _file_FRM, TypeFRM.DSP, this);// _frm.StartProgramm(_file_FRM, ProgrammFlash.TypeFRM.DSP);
            //result = await Task.Run(() =>
            //{
            //    return Programming(step, _file_FRM, TypeFRM.DSP, this);// _frm.StartProgramm(_file_FRM, ProgrammFlash.TypeFRM.DSP);
            //});
            if (result.state == PrgState.Finished)
                owner.UpdateStepLog(step, OperationStep.Programming, gridContainerBindingSource1, result.state, TypeFRM.DSP);       // Перепрограммирование DSP - Успех
            return result;
        }

        /// <summary>
        /// Метод, выполняющий перепрограммирование PLIS
        /// </summary>
        /// <param name="result">Объект с описанием текущего состояния</param>
        /// <returns></returns>
        private async Task<ProgrammState> ProgrammingPLIS(int step, MainForm owner)
        {
            ProgrammState result;
            owner.UpdateStepLog(step, OperationStep.Programming, gridContainerBindingSource1, PrgState.Processing, TypeFRM.PLIS);      // Начало прошивки ПЛИС

            result = await Programming(step, _file_PLIS, TypeFRM.PLIS, this);//_frm.StartProgramm(_file_PLIS, ProgrammFlash.TypeFRM.PLIS);

            if (result.state == PrgState.Finished)
                owner.UpdateStepLog(step, OperationStep.Programming, gridContainerBindingSource1, result.state, TypeFRM.PLIS);       // Перепрограммирование ПЛИС - Успех
            return result;
        }

        /// <summary>
        /// Метод, выполняющий перепрограммирование указанного типа вычислителя (DPS или PLIS) указанного файла
        /// </summary>
        /// <param name="result">Объект с описанием текущего состояния</param>
        /// <param name="file">Файл с прошивкой</param>
        /// <param name="type">Тип вычислителя</param>
        /// <returns></returns>
        private async Task<ProgrammState> Programming(int step, FileInfo file, TypeFRM type, MainForm owner)
        {
            ProgrammState result;
            int data_transfers = 0;

            result = await Task.Run(() =>
            {
                _frm.ResetAddresses();
                if (_frm.ConnectToMILSTD)
                {
                    using (StreamReader reader = new StreamReader(file.FullName))
                    {
                        int allLines = File.ReadAllLines(file.FullName).Count();
                        while (!reader.EndOfStream)
                        {
                            //System.Threading.Thread.Sleep(10);
                            _frm.ReadFileFRM(reader.ReadLine(), type, dataGridView1, (int)file.Length);

                            // Процессинг
                            dataGridView1.Invoke(new Action(() =>
                            {
                                owner.UpdateStepLog(step, OperationStep.Processing, gridContainerBindingSource1, PrgState.Processing, null, (int)(((double)data_transfers / allLines) * 100), "%");
                            }));
                            data_transfers++;
                        }
                        return new ProgrammState(PrgState.Finished);
                    }
                }
                else
                    return new ProgrammState(PrgState.MILSTDError, "Соединение с платой не было установлено");
            });
            return result;
        }

        /// <summary>
        /// Метод, очищающий подностью флеш-память
        /// </summary>
        /// <param name="result">Объект с описанием текущего состояния</param>
        /// <returns></returns>
        private async Task<ProgrammState> EraseFlashAll(int step, MainForm owner)
        {
            ProgrammState result;
            int countErrors = 0;
            owner.UpdateStepLog(step, OperationStep.ClearFlash, gridContainerBindingSource1, PrgState.Processing);              // Выставляю в таблице статус "Выполняется"
            result = await Task.Run(() =>
            {
                owner.UpdateStepLog(step, OperationStep.Processing, gridContainerBindingSource1, PrgState.Processing, null, 0, "c"); // Выставляю в таблице статус ход выполнения 0 c
                result = _frm.FlashCommand(CMD.EraseFlashALL, FORMATS_MILSTD.F1);

                if (result.state == PrgState.Finished)
                {

                    do
                    {
                        System.Threading.Thread.Sleep(1000); // Пауза 1 секунда
                        result = _frm.FlashCommand(CMD.EraseFlashALL, FORMATS_MILSTD.F2);
                        owner.UpdateStepLog(step, OperationStep.Processing, gridContainerBindingSource1, result.state, null, countErrors, "c"); // Выставляю в таблице статус ход выполнения c
                        countErrors++;
                        if (countErrors > 30) // в течение 30 с пытаюсь очистить флешку
                        {
                            result = new ProgrammState(PrgState.MILSTDError, String.Format("Ошибка при очистки флеш-памяти. Время ожидания отключения составило {0} с. \nПерепрошивка остановлена.", countErrors));
                            break;
                        }
                    }
                    while (result.state != PrgState.Finished);
                }

                return result;
            });

            if (result.state == PrgState.Finished)
                owner.UpdateStepLog(step, OperationStep.ClearFlash, gridContainerBindingSource1, result.state);                            // Откл. прерываний - Успех
            return result;
        }

        /// <summary>
        /// Метод, отключающий прерывания в АЭ
        /// </summary>
        /// <returns></returns>
        private async Task<ProgrammState> DisableInterrupt(int step, MainForm owner)
        {
            ProgrammState result;
            int countErrors = 0;
            owner.UpdateStepLog(step, OperationStep.InterruptOff, gridContainerBindingSource1, PrgState.Processing); // Выставляю в таблице статус "Выполняется"
            // Отключею прерывания (Ф1 и Ф2)
            result = await Task.Run(() =>
            {

                owner.UpdateStepLog(step, OperationStep.Processing, gridContainerBindingSource1, PrgState.Processing, null, 0, "c"); // Выставляю в таблице статус ход выполнения 0 %

                result = _frm.FlashCommand(CMD.DisableInterrupt, FORMATS_MILSTD.F1);
                if (result.state == PrgState.Finished)
                {

                    do
                    {
                        System.Threading.Thread.Sleep(200);
                        result = _frm.FlashCommand(CMD.DisableInterrupt, FORMATS_MILSTD.F2);
                        owner.UpdateStepLog(step, OperationStep.Processing, gridContainerBindingSource1, result.state, null, (countErrors * 200) / 1000, "c"); // Выставляю в таблице статус ход выполнения 50 %
                        countErrors++;
                        if (countErrors > 50) // в течение 10 с пытаюсь отключить прерывания
                        {
                            result = new ProgrammState(PrgState.MILSTDError, String.Format("Ошибка при отключении прерываний. Время ожидания отключения составило {0} с. \nПерепрошивка остановлена.", (countErrors * 200 / 1000)));
                            break;
                        }
                    }
                    while (result.state != PrgState.Finished);
                }

                return result;
            });

            if (result.state == PrgState.Finished)
                owner.UpdateStepLog(step, OperationStep.InterruptOff, gridContainerBindingSource1, result.state);                            // Откл. прерываний - Успех
            return result;
        }

        /// <summary>
        /// Изменение версии изделия
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox_version_CheckedChanged(object sender, EventArgs e)
        {
            //if (((CheckBox)sender).Checked)
            //    _frm.DefaultFRMAddressing = new ProgrammFlash.FRM_ADDR_SUB(10, 16);
            //else
            //    _frm.DefaultFRMAddressing = new ProgrammFlash.FRM_ADDR_SUB(10, 30);


            button_openPLISfile.Enabled = ((CheckBox)sender).Checked;
            button_progPLISfile.Enabled = ((CheckBox)sender).Checked;

            CheckVersion(checkBox_version.Checked);
        }

        /// <summary>
        /// Обработчик, проверяющий файл прошивки на соответствие формату IntelHex
        /// </summary>
        /// <param name="sender">Объект, порадивший событие</param>
        /// <param name="e">Аргументы состояния события</param>
        private void FRM_CausesValidationChanged(object sender, EventArgs e)
        {
            Cont control = sender as Cont;
            // Проверяю является ли объект, порадивший событие - TextBox'ом
            if (control != null)
            {
                // Извлекаю Провайдер соответствующий текущему TextBox
                // Очищаю ошибку у извлеченного Провайдера
                _checkErrors[control.textBox].errorProvider.Clear();
                try
                {
                    FileInfo checkFile;
                    TypeFRM type;
                    // В заивимости от объекта, порадившего событие, происходит анализ файла прошивки
                    if (control.textBox.Name.Contains(textBox_nameFRMfile.Name))
                    {
                        checkFile = _file_FRM;
                        type = TypeFRM.DSP;
                    }
                    else
                    {
                        checkFile = _file_PLIS;
                        type = TypeFRM.PLIS;
                    }

                    control.progFlashObj.IntelHexCheckFile(checkFile, type);
                    // Изменяю цвет текста в текущем TextBox на зеленый
                    TextBoxChangeForeColor(control.textBox, Color.Green);

                    // Устанавливаю, что с файлом все ОК
                    ProviderStateSwitch(control.textBox, _checkErrors[control.textBox], "Файл прошивки соответствует формату IntelHex", Icons.isOK, false);
                }
                catch (Exception err)
                {
                    // Изменяю цвет текста в текущем TextBox на красный
                    TextBoxChangeForeColor(control.textBox, Color.Red);

                    // Устанавливаю ошибку
                    ProviderStateSwitch(control.textBox, _checkErrors[control.textBox], err.Source + "\n" + err.Message, Icons.isError, true);

                    // MessageBox.Show(err.Source + "\n" + err.Message, "Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // return;
                }
                finally
                {
                    // Всегда устанавливаю стиль текста BOLD в TextBox
                    control.textBox.Font = new Font(control.textBox.Font, FontStyle.Bold);
                    ButtonProgEnableValidator(control);
                }
            }
        }

        /// <summary>
        /// Метод, контолирующий валидацию файла и "включающий" кнопки для перепрограммирования
        /// </summary>
        /// <param name="sender">Объект TextBox, порадивший событие</param>
        private void ButtonProgEnableValidator(TextBox sender)
        {
            // Прошиваем по старому (ПЛИС + DSP)
            if (checkBox_version.Checked)
            {
                // Прошиваем по старому (ПЛИС + DSP)
                bool statePrev = true;
                // Установка флага ошибки, когда обе прошивки верны
                foreach (KeyValuePair<TextBox, ErrorContainer> stateErrorValue in _checkErrors)
                {
                    _commonError = (statePrev || stateErrorValue.Value.errorState);
                    statePrev = stateErrorValue.Value.errorState;
                }
                if (_commonError)
                    _btn_old_version_prog.Enabled = false;
                else
                    _btn_old_version_prog.Enabled = true;
            }
            else
            {
                // Прошиваем по-новому
                System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(System.Text.RegularExpressions.Regex.Match(sender.Name, @"_\w+$").Value, @"[A-Z]\w+");
                System.Diagnostics.Debug.WriteLine("Найденное значение: {0}", m.Value);

                foreach (var cnt in this.Controls)
                    if ((cnt is CheckBox) && ((CheckBox)cnt).Name.Contains(m.Value))
                    {
                        if (!_checkErrors[sender].errorState)
                            ((CheckBox)cnt).Enabled = true; // Нет ошибки
                        else
                            ((CheckBox)cnt).Enabled = false;
                    }
            }
        }

        /// <summary>
        /// Метод, изменяющий состояние ErrorProvider, привязанного к конкретному TextBox
        /// </summary>
        /// <param name="sender">Элемент к которому привязан ErrorProvider</param>
        /// <param name="controlSenders">ErrorProvider, привязанный к элементу sender</param>
        /// <param name="ico">Иконка которую требуется отобразить</param>
        /// <param name="message">Сообщение, отображаемое ErrorProvider</param>
        private static void ProviderStateSwitch(TextBox sender, ErrorContainer controlSenders, string message, Icon ico = null, bool state = false)
        {
            if (ico == null)
                controlSenders.errorProvider.Icon = Icons.isError;
            else
                controlSenders.errorProvider.Icon = ico;
            // Установка состояния ошибки
            controlSenders.errorState = state;
            // Если файлы в порядке, то сбрасываю ошибку
            controlSenders.errorProvider.SetError(sender, message);
        }

        /// <summary>
        /// Изменение цвета текста в TextBox с включенной опцией ReadOnly
        /// </summary>
        /// <param name="control">Компонент TextBox у которого требуется изменить цвет текста</param>
        /// <param name="textColor">Требуемый цвет текста</param>
        private void TextBoxChangeForeColor(TextBox control, Color textColor)
        {
            control.ReadOnly = false;
            control.ForeColor = textColor;
            control.BackColor = Color.White;
            control.ReadOnly = true;
        }

        /// <summary>
        /// Кнопка запуска перепрограммирования PLIS
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_progPLISfile_CheckedChanged(object sender, EventArgs e)
        {
            // Отжатие кнопки только после успешного программирования!
            if (button_progPLISfile.Checked)
            {
                richTextBoxLog.Clear();
                toolStripProgressBar.Value = 0;
                toolStripProgressBar.Maximum = 11;
                button_progPLISfile.Enabled = false;

                // Начало программирования
                await StartProgramming(_file_PLIS, TypeFRM.PLIS);

                #region Комментарий
                ////richTextBoxLog.AppendText(toolStripStatusState.Text = String.Format("Установка адреса: {0} и подадреса: {1} \n", _frm.DefaultFRMAddressing.ADDR, _frm.DefaultFRMAddressing.SUB));
                //richTextBoxLog.AppendText(toolStripStatusState.Text = "Отключение прерываний в АЭ" + "\n");

                //// Отключею прерывания (Ф1 и Ф2)

                //ProgrammState result = await DisableInterrupt();                                                  // 1. Отключение прерываний

                //toolStripProgressBar.Increment(1);

                //if (result.state == PrgState.Finished)
                //{
                //    richTextBoxLog.AppendText(toolStripStatusState.Text = "Отключение прерываний в АЭ произведено успешно" + "\n");
                //    richTextBoxLog.AppendText(toolStripStatusState.Text = "Начата процедура очистки флеш-памяти АЭ" + "\n");
                //    // Стираю флешку целиком (ожидать долго надо и повторять каждую 1 секунду)

                //    for (ushort page = 0x00; page <= 0x08; page++)
                //    {
                //        result = await EraseFlashByPage(result, page);                                                          // 2. Очистка сектора флеш-памяти
                //        toolStripProgressBar.Increment(1);
                //    }

                //    if (result.state == PrgState.Finished)
                //    {

                //        richTextBoxLog.AppendText(toolStripStatusState.Text = "Флеш-память успешно очищена" + "\n");
                //        // Программирование ПЛИС
                //        richTextBoxLog.AppendText(toolStripStatusState.Text = "Начало программирования ПЛИС" + "\n");

                //        result = await ProgrammingPLIS(result);                                                                 // 3. Прошивка PLIS

                //        toolStripProgressBar.Increment(1);

                //        if (result.state == PrgState.Finished)
                //        {
                //            // Программирование ПЛИС
                //            richTextBoxLog.AppendText(toolStripStatusState.Text = "ПЛИС успешно перепрограммировано" + "\n");
                //            richTextBoxLog.AppendText(toolStripStatusState.Text = "Отключите питание АЭ" + "\n");
                //        }
                //    }
                //}
                //else
                //{
                //    richTextBoxLog.AppendText(result.message + "\n");
                //    MessageBox.Show(result.message);
                //}
                #endregion

                button_progPLISfile.CheckState = CheckState.Unchecked;
                button_progPLISfile.Enabled = true;
            }
        }

        private void dataGridView1_Resize(object sender, EventArgs e)
        {
            /// Изменение размера

            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                if (dataGridView1.Columns[i].DataPropertyName.Contains("NameField"))
                {
                    dataGridView1.Columns[i].Width = (int)((dataGridView1.Width - dataGridView1.RowHeadersWidth) * 0.75);
                }
                else
                {
                    dataGridView1.Columns[i].Width = (int)((dataGridView1.Width - dataGridView1.RowHeadersWidth) * (1 - 0.755));
                }
            }
        }

        /// <summary>
        /// Метод отображающий нужные кнопки в зависимости от типа изделия (версии)
        /// </summary>
        /// <param name="state">Состояние (state = true - скрыть, false - отобразить</param>
        private void CheckVersion(bool state)
        {
            // Скрываю или отображаю контролы в зависимости от состояния птицы
            button_progFRMfile.Enabled = button_progPLISfile.Enabled = _btn_old_version_prog.Enabled = false;
            button_progFRMfile.Visible = button_progPLISfile.Visible = !state;

            _btn_old_version_prog.Visible = state;

            if (!this.Controls.Contains(_btn_old_version_prog))
                this.Controls.Add(_btn_old_version_prog);
        }
    }
}

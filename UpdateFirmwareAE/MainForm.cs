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
using TKB995STD1553B;
namespace UpdateFirmwareAE
{
    public partial class MainForm : Form
    {
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

        public Manchester ml;
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

                richTextBoxLog.AppendText((toolStripStatusState.Text = "Отключение прерываний в АЭ") + "\n");                                 
                                                                                                                                              // ProgrammFlash.ProgrammState res;
                                                                                                                                              // Отключаю прерывания (Ф1 и Ф2)
                #region Старый код
                //ProgrammFlash.ProgrammState result = await Task.Run(() =>
                //{

                //    result = _frm.FlashCommand(ProgrammFlash.CMD.DisableInterrupt, FORMATS_MILSTD.F1);
                //    if (result.state == ProgrammFlash.PrgState.Finished)
                //    {
                //        do
                //        {
                //            System.Threading.Thread.Sleep(200);
                //            result = _frm.FlashCommand(ProgrammFlash.CMD.DisableInterrupt, FORMATS_MILSTD.F2);
                //        }
                //        while (result.state != ProgrammFlash.PrgState.Finished);
                //    }

                //    return result;
                //});

                //if (result.state == ProgrammFlash.PrgState.Finished)
                //{
                //    richTextBoxLog.AppendText((toolStripStatusState.Text = "Отключение прерываний в АЭ произведено успешно") + "\n");
                //    richTextBoxLog.AppendText((toolStripStatusState.Text = "Начата процедура очистки флеш-памяти страниц DSP") + "\n");
                //    // Стираю флешку целиком (ожидать долго надо и повторять каждую 1 секунду)
                //    result = await Task.Run(() =>
                //    {
                //        // Обход всех страниц
                //        for (ushort page = 0xA; page <= 0xB; page++)
                //        {
                //            do
                //            {
                //                result = _frm.FlashCommand(ProgrammFlash.CMD.EraseFlashSector, FORMATS_MILSTD.F1, page);                       // 2. Очистка секторов
                //            }
                //            while (result.state != ProgrammFlash.PrgState.Finished);

                //            do
                //            {
                //                System.Threading.Thread.Sleep(1000); // Пауза 1 секунда
                //                result = _frm.FlashCommand(ProgrammFlash.CMD.EraseFlashSector, FORMATS_MILSTD.F2);
                //            }
                //            while (result.state != ProgrammFlash.PrgState.Finished);

                //        }
                //        return new ProgrammFlash.ProgrammState(ProgrammFlash.PrgState.Finished, "Очистка секторов успешна!");
                //    });


                //    if (result.state == ProgrammFlash.PrgState.Finished)
                //    {
                //        richTextBoxLog.AppendText((toolStripStatusState.Text = "Страницы флеш-памяти успешно очищены") + "\n");
                //        richTextBoxLog.AppendText((toolStripStatusState.Text = "Начало программирования DSP") + "\n");

                //        result = await Task.Run(() =>
                //        {
                //            return _frm.StartProgramm(_file_FRM, ProgrammFlash.TypeFRM.DSP);                                                // 3. Прошивка DSP
                //        });

                //        if (result.state == ProgrammFlash.PrgState.Finished)
                //        {
                //            // Программирование DPS
                //            richTextBoxLog.AppendText((toolStripStatusState.Text = "DSP успешно перепрограммировано") + "\n");
                //            richTextBoxLog.AppendText((toolStripStatusState.Text = "Отключите питание АЭ") + "\n");
                //        }
                //    }
                //}
                //else
                //{
                //    richTextBoxLog.AppendText(result.message + "\n");
                //    MessageBox.Show(result.message);
                //}
                #endregion

                ProgrammFlash.ProgrammState result = await DisableInterrupt();                                                  // 1. Отключение прерываний
                toolStripProgressBar.Increment(1);

                if (result.state == ProgrammFlash.PrgState.Finished)
                {
                    richTextBoxLog.AppendText((toolStripStatusState.Text = "Отключение прерываний в АЭ произведено успешно") + "\n");
                    richTextBoxLog.AppendText((toolStripStatusState.Text = "Начата процедура очистки флеш-памяти страниц DSP") + "\n");
                    // Стираю флешку целиком (ожидать долго надо и повторять каждую 1 секунду)
                    for (ushort page = 0xA; page <= 0xB; page++)
                    {
                        result = await EraseFlashByPage(result, page);                                                          // 2. Очистка сектора флеш-памяти
                        toolStripProgressBar.Increment(1);
                    }

                    if (result.state == ProgrammFlash.PrgState.Finished)
                    {
                        richTextBoxLog.AppendText((toolStripStatusState.Text = "Страницы флеш-памяти успешно очищены") + "\n");
                        richTextBoxLog.AppendText((toolStripStatusState.Text = "Начало программирования DSP") + "\n");

                        result = await ProgrammingDSP(result);                                                                  // 3. Прошивка DSP
                        toolStripProgressBar.Increment(1);

                        if (result.state == ProgrammFlash.PrgState.Finished)
                        {
                            // Программирование DPS
                            richTextBoxLog.AppendText((toolStripStatusState.Text = "DSP успешно перепрограммировано") + "\n");
                            richTextBoxLog.AppendText((toolStripStatusState.Text = "Отключите питание АЭ") + "\n");
                        }
                    }
                }
                else
                {
                    richTextBoxLog.AppendText((toolStripStatusState.Text = result.message) + "\n");
                    MessageBox.Show(result.message);
                }
                
                button_progFRMfile.CheckState = CheckState.Unchecked;
                button_progFRMfile.Enabled = true;
            }
        }

        private async Task<ProgrammFlash.ProgrammState> EraseFlashByPage(ProgrammFlash.ProgrammState result, ushort page)
        {
            result = await Task.Run(() =>
            {
                // Обход всех страниц

                do
                {
                    result = _frm.FlashCommand(ProgrammFlash.CMD.EraseFlashSector, FORMATS_MILSTD.F1, page);                       // 2. Очистка секторов
                }
                while (result.state != ProgrammFlash.PrgState.Finished);

                do
                {
                    System.Threading.Thread.Sleep(1000); // Пауза 1 секунда
                    result = _frm.FlashCommand(ProgrammFlash.CMD.EraseFlashSector, FORMATS_MILSTD.F2);
                }
                while (result.state != ProgrammFlash.PrgState.Finished);


                return new ProgrammFlash.ProgrammState(result, "Очистка секторов успешна!");
            });
            return result;
        }

        /// <summary>
        /// Метод, загружающий форму
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            GenErrorProviders();                    // Создание словаря TextBox - ErrorProvider
            CheckVersion(checkBox_version.Checked);
            _frm = new ProgrammFlash();
            _frm.DefaultFRMAddressing = new ProgrammFlash.FRM_ADDR_SUB(10, 30);
            if (_frm.ConnectToMILSTD == false)
            {
                MessageBox.Show("Связь с платой интерфейса MILSTD-1553B производства Элкус не установлена!", "Внимание!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
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

        private async void btn_old_version_prog_Click(object sender, EventArgs e)
        {
            CheckBox currCheck = sender as CheckBox;
            if ((currCheck!= null) && (currCheck.Checked))
            {
                _frm.DefaultFRMAddressing = new ProgrammFlash.FRM_ADDR_SUB(10, 16);

                richTextBoxLog.Clear();
                toolStripProgressBar.Value = 0;
                toolStripProgressBar.Maximum = 4;
                currCheck.Enabled = false;

                richTextBoxLog.AppendText(toolStripStatusState.Text = "Отключение прерываний в АЭ" + "\n");



                ProgrammFlash.ProgrammState result = await DisableInterrupt();                                                  // 1. Отключение прерываний
                toolStripProgressBar.Increment(1);

                if (result.state == ProgrammFlash.PrgState.Finished)
                {
                    richTextBoxLog.AppendText(toolStripStatusState.Text = "Отключение прерываний в АЭ произведено успешно" + "\n");
                    richTextBoxLog.AppendText(toolStripStatusState.Text = "Начата процедура очистки флеш-памяти АЭ" + "\n");
                    // Стираю флешку целиком (ожидать долго надо и повторять каждую 1 секунду)

                    result = await EraseFlashAll(result);                                                                       // 2. Очистка флешки
                    toolStripProgressBar.Increment(1);

                    if (result.state == ProgrammFlash.PrgState.Finished)
                    {

                        richTextBoxLog.AppendText(toolStripStatusState.Text = "Флеш-память успешно очищена" + "\n");

                        // Программирование ПЛИС
                        richTextBoxLog.AppendText(toolStripStatusState.Text = "Начало программирования ПЛИС" + "\n");

                        result = await ProgrammingPLIS(result);                                                                 // 3. Прошивка PLIS
                        toolStripProgressBar.Increment(1);

                        if (result.state == ProgrammFlash.PrgState.Finished)
                        {
                            // Программирование ПЛИС
                            richTextBoxLog.AppendText(toolStripStatusState.Text = "ПЛИС успешно перепрограммировано" + "\n");
                            // Программирование DPS
                            richTextBoxLog.AppendText(toolStripStatusState.Text = "Начало программирования DSP" + "\n");

                            result = await ProgrammingDSP(result);                                                              // 4. Прошивка DSP
                            toolStripProgressBar.Increment(1);

                            if (result.state == ProgrammFlash.PrgState.Finished)
                            {
                                // Программирование DPS
                                richTextBoxLog.AppendText(toolStripStatusState.Text = "DPS успешно перепрограммировано" + "\n");
                                richTextBoxLog.AppendText(toolStripStatusState.Text = "Отключите питание АЭ" + "\n");
                            }
                        }
                    }
                }
                else
                {
                    richTextBoxLog.AppendText(result.message + "\n");
                    MessageBox.Show(result.message);
                }
            }
            richTextBoxLog.ScrollToCaret();
        }

        /// <summary>
        /// Метод, выполняющий перепрограммирование DSP
        /// </summary>
        /// <param name="result">Объект с описанием текущего состояния</param>
        /// <returns></returns>
        private async Task<ProgrammFlash.ProgrammState> ProgrammingDSP(ProgrammFlash.ProgrammState result)
        {
            result = await Task.Run(() =>
            {
                return Programming(result, _file_FRM, ProgrammFlash.TypeFRM.DSP);// _frm.StartProgramm(_file_FRM, ProgrammFlash.TypeFRM.DSP);
            });
            return result;
        }

        /// <summary>
        /// Метод, выполняющий перепрограммирование PLIS
        /// </summary>
        /// <param name="result">Объект с описанием текущего состояния</param>
        /// <returns></returns>
        private async Task<ProgrammFlash.ProgrammState> ProgrammingPLIS(ProgrammFlash.ProgrammState result)
        {
            result = await Task.Run(() =>
            {
                return Programming(result, _file_PLIS, ProgrammFlash.TypeFRM.PLIS);//_frm.StartProgramm(_file_PLIS, ProgrammFlash.TypeFRM.PLIS);
            });
            return result;
        }

        /// <summary>
        /// Метод, выполняющий перепрограммирование указанного типа вычислителя (DPS или PLIS) указанного файла
        /// </summary>
        /// <param name="result">Объект с описанием текущего состояния</param>
        /// <param name="file">Файл с прошивкой</param>
        /// <param name="type">Тип вычислителя</param>
        /// <returns></returns>
        private async Task<ProgrammFlash.ProgrammState> Programming(ProgrammFlash.ProgrammState result, FileInfo file, ProgrammFlash.TypeFRM type)
        {
            result = await Task.Run(() =>
            {
                return _frm.StartProgramm(file, type);
            });
            return result;
        }

        /// <summary>
        /// Метод, очищающий подностью флеш-память
        /// </summary>
        /// <param name="result">Объект с описанием текущего состояния</param>
        /// <returns></returns>
        private async Task<ProgrammFlash.ProgrammState> EraseFlashAll(ProgrammFlash.ProgrammState result)
        {
            result = await Task.Run(() =>
            {
                result = _frm.FlashCommand(ProgrammFlash.CMD.EraseFlashALL, FORMATS_MILSTD.F1);
                if (result.state == ProgrammFlash.PrgState.Finished)
                {
                    do
                    {
                        System.Threading.Thread.Sleep(1000); // Пауза 1 секунда
                        result = _frm.FlashCommand(ProgrammFlash.CMD.EraseFlashALL, FORMATS_MILSTD.F2);

                    }
                    while (result.state != ProgrammFlash.PrgState.Finished);
                }

                return result;
            });
            return result;
        }

        /// <summary>
        /// Метод, отключающий прерывания в АЭ
        /// </summary>
        /// <returns></returns>
        private async Task<ProgrammFlash.ProgrammState> DisableInterrupt()
        {
            ProgrammFlash.ProgrammState result;

            // Отключею прерывания (Ф1 и Ф2)
            return await Task.Run(() =>
            {
                result = _frm.FlashCommand(ProgrammFlash.CMD.DisableInterrupt, FORMATS_MILSTD.F1);
                if (result.state == ProgrammFlash.PrgState.Finished)
                {
                    do
                    {
                        System.Threading.Thread.Sleep(200);
                        result = _frm.FlashCommand(ProgrammFlash.CMD.DisableInterrupt, FORMATS_MILSTD.F2);
                    }
                    while (result.state != ProgrammFlash.PrgState.Finished);
                }

                return result;
            });
        }

        /// <summary>
        /// Изменение версии изделия
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox_version_CheckedChanged(object sender, EventArgs e)
        {
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
                    // В заивимости от объекта, порадившего событие, происходит анализ файла прошивки
                    if (control.textBox.Name.Contains(textBox_nameFRMfile.Name))
                        checkFile = _file_FRM;
                    else
                        checkFile = _file_PLIS;


                    control.progFlashObj.IntelHexCheckFile(checkFile);
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

                //richTextBoxLog.AppendText(toolStripStatusState.Text = String.Format("Установка адреса: {0} и подадреса: {1} \n", _frm.DefaultFRMAddressing.ADDR, _frm.DefaultFRMAddressing.SUB));
                richTextBoxLog.AppendText(toolStripStatusState.Text = "Отключение прерываний в АЭ" + "\n");

                // Отключею прерывания (Ф1 и Ф2)

                ProgrammFlash.ProgrammState result = await DisableInterrupt();                                                  // 1. Отключение прерываний

                toolStripProgressBar.Increment(1);

                if (result.state == ProgrammFlash.PrgState.Finished)
                {
                    richTextBoxLog.AppendText(toolStripStatusState.Text = "Отключение прерываний в АЭ произведено успешно" + "\n");
                    richTextBoxLog.AppendText(toolStripStatusState.Text = "Начата процедура очистки флеш-памяти АЭ" + "\n");
                    // Стираю флешку целиком (ожидать долго надо и повторять каждую 1 секунду)

                    for (ushort page = 0x00; page <= 0x08; page++)
                    {
                        result = await EraseFlashByPage(result, page);                                                          // 2. Очистка сектора флеш-памяти
                        toolStripProgressBar.Increment(1);
                    }

                    if (result.state == ProgrammFlash.PrgState.Finished)
                    {

                        richTextBoxLog.AppendText(toolStripStatusState.Text = "Флеш-память успешно очищена" + "\n");
                        // Программирование ПЛИС
                        richTextBoxLog.AppendText(toolStripStatusState.Text = "Начало программирования ПЛИС" + "\n");

                        result = await ProgrammingPLIS(result);                                                                 // 3. Прошивка PLIS

                        toolStripProgressBar.Increment(1);

                        if (result.state == ProgrammFlash.PrgState.Finished)
                        {
                            // Программирование ПЛИС
                            richTextBoxLog.AppendText(toolStripStatusState.Text = "ПЛИС успешно перепрограммировано" + "\n");
                            richTextBoxLog.AppendText(toolStripStatusState.Text = "Отключите питание АЭ" + "\n");
                        }
                    }
                }
                else
                {
                    richTextBoxLog.AppendText(result.message + "\n");
                    MessageBox.Show(result.message);
                }

                button_progPLISfile.CheckState = CheckState.Unchecked;
                button_progPLISfile.Enabled = true;
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

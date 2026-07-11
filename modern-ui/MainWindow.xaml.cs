using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IOPath = System.IO.Path;
using Drawing = System.Drawing;
using WinForms = System.Windows.Forms;

namespace RawAccelModern
{
    public partial class MainWindow : Window
    {
        private string rootDirectory;
        private string settingsPath;
        private JObject settings;
        private bool loading;
        private int displayDpi = 800;
        private int pollRate = 1000;
        private HwndSource windowSource;
        private long lastRawTimestamp;
        private long lastMarkerTimestamp;
        private double smoothedOutputSpeed;
        private double smoothedInputSpeed;
        private bool hasSmoothedSpeed;
        private List<double> displayedCurve = new List<double>();
        private double chartPlotLeft;
        private double chartPlotTop;
        private double chartPlotWidth;
        private double chartPlotHeight;
        private double chartYMin;
        private double chartYMax;
        private double lastMarkerInputSpeed;
        private double lastMarkerRatio;
        private bool hasMouseMarker;
        private Ellipse mouseMarker;
        private Line mouseGuide;
        private Border mouseMarkerLabel;
        private WinForms.NotifyIcon trayIcon;
        private WinForms.ToolStripMenuItem trayOpenItem;
        private WinForms.ToolStripMenuItem trayExitItem;
        private bool allowApplicationExit;
        private bool trayMessageShown;
        private WindowState stateBeforeTray = WindowState.Maximized;
        private string currentLanguage = "en";
        private bool changingLanguage;
        private readonly HashSet<string> ignoredDeviceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int lastMouseDx;
        private int lastMouseDy;

        private static readonly Dictionary<string, string> PortugueseText = new Dictionary<string, string>
        {
            { "Connected Devices", "Dispositivos conectados" },
            { "Read-only mouse detection; no driver settings are changed", "Detecção de mouse somente leitura; nenhuma configuração do driver é alterada" },
            { "Detect mice and associate profiles safely", "Detecte mouses e associe perfis com segurança" },
            { "Waiting for device scan", "Aguardando detecção de dispositivos" },
            { "Refresh Devices", "Atualizar dispositivos" },
            { "No mouse devices were detected.", "Nenhum dispositivo de mouse foi detectado." },
            { "Detection failed", "Falha na detecção" },
            { "Connected", "Conectado" },
            { "Uses default settings", "Usa configurações padrão" },
            { "Specific configuration found", "Configuração específica encontrada" },
            { "Device identifier", "Identificador do dispositivo" },
            { "Associate Profile", "Associar perfil" },
            { "Use Default", "Usar padrão" },
            { "Assigned profile", "Perfil associado" },
            { "Device profile applied", "Perfil do dispositivo aplicado" },
            { "Device returned to default settings", "Dispositivo retornou às configurações padrão" },
            { "Device association was not applied", "A associação do dispositivo não foi aplicada" },
            { "Device Association", "Associação de dispositivo" },
            { "Show ignored devices", "Mostrar dispositivos ignorados" },
            { "Not a Mouse", "Não é mouse" },
            { "Restore Device", "Restaurar dispositivo" },
            { "Ignored in this app", "Ignorado neste aplicativo" },
            { "All detected devices are ignored.", "Todos os dispositivos detectados estão ignorados." },
            { "Profile Management", "Gerenciamento de perfis" },
            { "Create a clean profile or duplicate the selected profile", "Crie um perfil limpo ou duplique o perfil selecionado" },
            { "Selected profile", "Perfil selecionado" },
            { "New profile name", "Nome do novo perfil" },
            { "Create Clean Profile", "Criar perfil limpo" },
            { "Duplicate Selected", "Duplicar selecionado" },
            { "Profile created", "Perfil criado" },
            { "Profile duplicated", "Perfil duplicado" },
            { "Profile operation failed", "Falha na operação de perfil" },
            { "Profile name is required.", "O nome do perfil é obrigatório." },
            { "A profile with this name already exists.", "Já existe um perfil com este nome." },
            { "Profile name contains invalid characters.", "O nome do perfil contém caracteres inválidos." },
            { "Smoothing & Stability", "Suavização e estabilidade" },
            { "Controls acceleration stability and abnormal speed spikes", "Controla a estabilidade da aceleração e picos anormais de velocidade" },
            { "Input Smoothing (ms)", "Suavização de entrada (ms)" },
            { "Sensitivity Smoothing (ms)", "Suavização da sensibilidade (ms)" },
            { "Output Smoothing (ms)", "Suavização de saída (ms)" },
            { "Input Speed Cap", "Limite da velocidade de entrada" },
            { "0 disables each option. Output smoothing can add input latency.", "0 desativa cada opção. A suavização de saída pode adicionar latência." },
            { "Apply Smoothing Settings", "Aplicar suavização" },
            { "Smoothing settings applied", "Configurações de suavização aplicadas" },
            { "Smoothing settings were not applied", "As configurações de suavização não foram aplicadas" },
            { "Smoothing values must be between 0 and 100 ms.", "Os valores de suavização devem estar entre 0 e 100 ms." },
            { "Input Speed Cap must be 0 or between 0.10 and 1000.00 counts/ms.", "O limite da velocidade de entrada deve ser 0 ou estar entre 0,10 e 1000,00 counts/ms." },
            { "Input speed calculation parameters were not found.", "Os parâmetros de cálculo da velocidade de entrada não foram encontrados." },
            { "Vertical Response", "Resposta vertical" },
            { "Axis Tuning", "Ajuste de eixos" },
            { "Directional sensitivity and vertical response", "Sensibilidade direcional e resposta vertical" },
            { "Directional controls not shown on Charts", "Controles direcionais não exibidos em Gráficos" },
            { "Base Y/X Ratio", "Proporção base Y/X" },
            { "Left / Right Ratio", "Proporção esquerda / direita" },
            { "Up / Down Ratio", "Proporção cima / baixo" },
            { "Rotation (degrees)", "Rotação (graus)" },
            { "Angle Snapping (degrees)", "Ajuste angular (graus)" },
            { "Values above 1.00 increase the named direction. Rotation and snapping are limited for safety.", "Valores acima de 1,00 aumentam a direção indicada. A rotação e o ajuste angular são limitados por segurança." },
            { "Values above 1.00 increase the named direction. Angle snapping is limited for safety.", "Valores acima de 1,00 aumentam a direção indicada. O ajuste angular é limitado por segurança." },
            { "Apply Axis Settings", "Aplicar ajustes de eixo" },
            { "Axis settings applied", "Ajustes de eixo aplicados" },
            { "Axis settings were not applied", "Os ajustes de eixo não foram aplicados" },
            { "Directional ratios must be between 0.10 and 10.00.", "As proporções direcionais devem estar entre 0,10 e 10,00." },
            { "Vertical Activation and Strength must be between 0.10 and 5.00.", "A Ativação vertical e a Intensidade vertical devem estar entre 0,10 e 5,00." },
            { "Rotation must be between -180 and 180 degrees.", "A rotação deve estar entre -180 e 180 graus." },
            { "Angle Snapping must be between 0 and 45 degrees.", "O ajuste angular deve estar entre 0 e 45 graus." },
            { "The selected profile was not found.", "O perfil selecionado não foi encontrado." },
            { "Charts", "Gráficos" },
            { "Advanced", "Avançado" },
            { "Themes", "Temas" },
            { "Help", "Ajuda" },
            { "Acceleration", "Aceleração" },
            { "Last (x, y): (0, 0)", "Último (x, y): (0, 0)" },
            { "Current", "Atual" },
            { "Lock", "Travar" },
            { "Sens Multiplier", "Multiplicador de sensibilidade" },
            { "Y / X Ratio", "Proporção Y / X" },
            { "Rotation", "Rotação" },
            { "Curve / Profile", "Curva / Perfil" },
            { "Natural", "Natural" },
            { "Classic", "Clássico" },
            { "Jump", "Salto" },
            { "Synchronous", "Síncrono" },
            { "Power", "Potência" },
            { "No Accel", "Sem aceleração" },
            { "Gain", "Ganho" },
            { "Decay Rate", "Taxa de decaimento" },
            { "Input Offset", "Deslocamento de entrada" },
            { "Limit", "Limite" },
            { "Vertical Activation", "Ativação vertical" },
            { "Starts vertical accel earlier", "Inicia a aceleração vertical mais cedo" },
            { "Vertical Accel Strength", "Intensidade da aceleração vertical" },
            { "Extra vertical curve range", "Aumenta a faixa da curva vertical" },
            { "Decimals: 1.05 or 1,05", "Decimais: 1.05 ou 1,05" },
            { "Apply", "Aplicar" },
            { "Reset", "Redefinir" },
            { "Sensitivity", "Sensibilidade" },
            { "Accelerated Sensitivity", "Sensibilidade acelerada" },
            { "Last Mouse Move", "Último movimento do mouse" },
            { "Ratio", "Proporção" },
            { "Output @ 10", "Saída @ 10" },
            { "Input Speed (counts/ms)", "Velocidade de entrada (counts/ms)" },
            { "Ratio of Output to Input", "Proporção entre saída e entrada" },
            { "Driver status:", "Status do driver:" },
            { "Input:", "Entrada:" },
            { "Output:", "Saída:" },
            { "Device & Input", "Dispositivo e entrada" },
            { "Default settings used for connected mouse devices", "Configurações padrão para os mouses conectados" },
            { "Mouse DPI", "DPI do mouse" },
            { "0 disables DPI normalization", "0 desativa a normalização de DPI" },
            { "Polling Rate (Hz)", "Taxa de polling (Hz)" },
            { "0 uses automatic detection; valid manual range 125–8000", "0 usa detecção automática; faixa manual válida 125–8000" },
            { "Use a constant interval based on polling rate", "Usar intervalo constante baseado na taxa de polling" },
            { "Disable acceleration for default devices", "Desativar aceleração nos dispositivos padrão" },
            { "DPI normalization changes the speed unit used by the curve. Polling rate should remain 0 unless automatic timing causes stutter. These settings do not change the acceleration formulas.", "A normalização de DPI altera a unidade de velocidade usada pela curva. Mantenha o polling em 0, exceto se a detecção automática causar travamentos. Estas opções não alteram as fórmulas de aceleração." },
            { "Apply Device Settings", "Aplicar configurações do dispositivo" },
            { "Driver Control", "Controle do driver" },
            { "Communication and profile activation", "Comunicação e ativação do perfil" },
            { "Not tested", "Não testado" },
            { "Test Driver Communication", "Testar comunicação do driver" },
            { "Apply Current Profile", "Aplicar perfil atual" },
            { "Disable Acceleration", "Desativar aceleração" },
            { "Advanced — Stage 1", "Avançado — Etapa 1" },
            { "This first stage contains only device timing and safe driver controls. Axis tuning, smoothing and per-device profiles will be added and tested separately.", "Esta primeira etapa contém apenas temporização do dispositivo e controles seguros do driver. Ajustes de eixo, suavização e perfis por dispositivo serão adicionados e testados separadamente." },
            { "Ready", "Pronto" },
            { "Active", "Ativo" },
            { "Disabled", "Desativado" },
            { "Configuration error", "Erro de configuração" },
            { "Apply failed", "Falha ao aplicar" },
            { "Connected — acceleration active", "Conectado — aceleração ativa" },
            { "Connected — acceleration disabled", "Conectado — aceleração desativada" },
            { "Connected — current profile applied", "Conectado — perfil atual aplicado" },
            { "Device settings applied", "Configurações do dispositivo aplicadas" },
            { "Values reloaded from settings.json", "Valores recarregados do settings.json" },
            { "Device settings were not applied", "As configurações do dispositivo não foram aplicadas" },
            { "Driver communication failed", "Falha na comunicação com o driver" },
            { "Profile apply failed", "Falha ao aplicar o perfil" },
            { "Could not disable acceleration", "Não foi possível desativar a aceleração" },
            { "Open", "Abrir" },
            { "Close (disable acceleration)", "Fechar (desativar aceleração)" },
            { "Raw Accel is still active", "Raw Accel continua ativo" },
            { "Double-click to open or right-click to close.", "Clique duas vezes para abrir ou use o botão direito para fechar." },
            { "The modern interface uses the reference palette. Classic themes remain available in the original rawaccel.exe.", "A interface moderna usa a paleta de referência. Os temas clássicos continuam disponíveis no rawaccel.exe original." },
            { "Unable to load the Raw Accel configuration.", "Não foi possível carregar a configuração do Raw Accel." },
            { "The program was not closed because it could not disable acceleration in the driver.", "O programa não foi encerrado porque não conseguiu desativar a aceleração no driver." },
            { "No configuration was applied.", "Nenhuma configuração foi aplicada." },
            { "Invalid value in {0}.", "Valor inválido em {0}." },
            { "{0} must be an integer.", "{0} deve ser um número inteiro." },
            { "{0} is outside the allowed range.", "{0} está fora do intervalo permitido." },
            { "Mouse DPI must be 0 or between 50 and 100000.", "O DPI do mouse deve ser 0 ou estar entre 50 e 100000." },
            { "Polling Rate must be 0 or between 125 and 8000 Hz.", "A taxa de polling deve ser 0 ou estar entre 125 e 8000 Hz." },
            { "Enter a Polling Rate to use a constant interval.", "Informe uma taxa de polling para usar intervalo constante." },
            { "The original engine rejected the configuration.", "O motor original recusou a configuração." },
            { "Could not start writer.exe.", "Não foi possível iniciar writer.exe." },
            { "The driver did not respond within the expected time.", "O driver não respondeu dentro do tempo esperado." }
            ,{ "Unable to connect the window to Raw Input.", "Não foi possível conectar a janela ao Raw Input." }
            ,{ "Windows rejected Raw Input registration.", "O Windows recusou o registro de Raw Input." }
            ,{ "No profiles were found in settings.json.", "Nenhum perfil foi encontrado em settings.json." }
            ,{ "defaultDeviceConfig was not found.", "defaultDeviceConfig não foi encontrado." }
            ,{ "The configuration was rejected by the original Raw Accel engine.", "A configuração foi recusada pelo motor original do Raw Accel." }
        };

        private static readonly Dictionary<string, string> HelpTextEnglish = new Dictionary<string, string>
        {
            { "Connected Devices", "Lists mouse-capable HID interfaces using the original Raw Accel enumerator. Some keyboards also expose a mouse interface; use Not a Mouse to hide them locally. Refresh and ignore do not edit settings.json. Profile association requires confirmation, backup and complete validation." },
            { "Profile Management", "Create Clean Profile adds a new Raw Accel default profile with acceleration disabled. Duplicate Selected copies every value from the profile currently selected at the top. Both actions validate the complete configuration, create a backup and apply it to the driver." },
            { "Input Smoothing (ms)", "Smooths the input values used to calculate mouse speed and acceleration. Higher half-life values reduce sensor noise but make acceleration react more slowly. Start between 1 and 3 ms; 0 disables it." },
            { "Sensitivity Smoothing (ms)", "Smooths rapid changes in the acceleration multiplier without directly averaging the final cursor movement. It can make transitions steadier with less latency than output smoothing. Start between 1 and 3 ms; 0 disables it." },
            { "Output Smoothing (ms)", "Averages the final mouse output. This can make movement look smoother, but it adds direct input latency and reduces the immediate connection to the mouse. Keep it at 0 for competitive aiming." },
            { "Input Speed Cap", "Limits the speed value used by the acceleration calculation to prevent abnormal sensor spikes from jumping far along the curve. 0 disables the cap. The unit is counts per millisecond." },
            { "Sens Multiplier", "Multiplies the final mouse output at every speed. 1.00 keeps the base sensitivity; 1.20 makes all movement about 20% faster." },
            { "Y / X Ratio", "Changes vertical sensitivity relative to horizontal sensitivity, equally for up and down. 1.05 makes vertical output about 5% faster." },
            { "Rotation", "Rotates the reference axes used by horizontal, vertical and directional adjustments. Keep it at 0 unless your natural mouse movement is tilted." },
            { "Curve / Profile", "Selects the mathematical acceleration curve. Natural is smooth and progressive; the other modes produce different curve shapes and may use parameters differently." },
            { "Gain", "When enabled, the selected shape is applied as gain—the rate at which output velocity changes—instead of directly as sensitivity. This can significantly change the feel of the same curve." },
            { "Decay Rate", "Controls how quickly the Natural curve rises toward its limit after acceleration begins. Higher values make the transition happen sooner and more aggressively." },
            { "Input Offset", "Sets the input speed threshold before acceleration begins. Below this speed, movement stays close to the base sensitivity." },
            { "Limit", "Sets the upper strength or ceiling approached by the acceleration curve. Higher values allow a larger difference between slow and fast movements." },
            { "Vertical Activation", "Changes how quickly vertical movement advances through the acceleration curve. Above 1.00 makes vertical acceleration activate earlier; below 1.00 makes it activate later. It affects both up and down." },
            { "Vertical Accel Strength", "Scales only the accelerated part of vertical movement. Above 1.00 makes fast vertical movement gain more acceleration without directly increasing the base vertical sensitivity." },
            { "Mouse DPI", "Normalizes input speed using the physical DPI of the mouse. Set the real DPI to make profiles more consistent between mice; use 0 to disable normalization." },
            { "Polling Rate (Hz)", "Defines the expected mouse report rate. Leave it at 0 for automatic detection. Set a manual value only when automatic timing causes stutter or inconsistent acceleration." },
            { "Use a constant interval based on polling rate", "Forces calculations to use a fixed time interval derived from the configured polling rate. It requires a manual polling rate and is mainly a troubleshooting option." },
            { "Disable acceleration for default devices", "Prevents acceleration on devices that do not have a specific device profile. Enable it only when you want Raw Accel active exclusively on configured devices." },
            { "Test Driver Communication", "Checks whether the application can read the active configuration from the Raw Accel driver. It does not change your profile." },
            { "Apply Current Profile", "Sends the current settings.json profile to the driver again. Use it after external edits or if the driver is active with an older configuration." },
            { "Disable Acceleration", "Deactivates acceleration in the driver immediately. The saved profile remains available and can be applied again later." },
            { "Left / Right Ratio", "Changes leftward sensitivity relative to rightward sensitivity. 1.00 keeps both equal; 1.05 makes left movement about 5% faster than right." },
            { "Up / Down Ratio", "Changes upward sensitivity relative to downward sensitivity. 1.00 keeps both equal; 1.05 makes upward movement about 5% faster than downward movement." },
            { "Angle Snapping (degrees)", "Straightens movements that are close to the horizontal or vertical axes. 0 disables it; 1–3 is subtle; high values strongly correct the movement and can reduce free aim." }
        };

        private static readonly Dictionary<string, string> HelpTextPortuguese = new Dictionary<string, string>
        {
            { "Connected Devices", "Lista interfaces HID capazes de gerar movimentos de mouse usando o enumerador original do Raw Accel. Alguns teclados também expõem uma interface de mouse; use Não é mouse para ocultá-los localmente. Atualizar e ignorar não editam o settings.json. Associar perfil exige confirmação, backup e validação completa." },
            { "Profile Management", "Criar perfil limpo adiciona um novo perfil padrão do Raw Accel com aceleração desativada. Duplicar selecionado copia todos os valores do perfil escolhido no topo. As duas ações validam a configuração completa, criam backup e aplicam ao driver." },
            { "Input Smoothing (ms)", "Suaviza os valores de entrada usados para calcular a velocidade e a aceleração do mouse. Tempos maiores reduzem ruídos do sensor, mas fazem a aceleração reagir mais lentamente. Comece entre 1 e 3 ms; 0 desativa." },
            { "Sensitivity Smoothing (ms)", "Suaviza mudanças rápidas no multiplicador de aceleração sem calcular uma média direta do movimento final. Pode estabilizar as transições com menos latência que a suavização de saída. Comece entre 1 e 3 ms; 0 desativa." },
            { "Output Smoothing (ms)", "Calcula uma média da saída final do mouse. O movimento pode parecer mais suave, mas isso adiciona latência direta e reduz a resposta imediata. Mantenha em 0 para jogos competitivos." },
            { "Input Speed Cap", "Limita a velocidade usada no cálculo da aceleração para impedir que picos anormais do sensor avancem demais pela curva. 0 desativa o limite. A unidade é counts por milissegundo." },
            { "Sens Multiplier", "Multiplica a saída final do mouse em qualquer velocidade. 1,00 mantém a sensibilidade base; 1,20 deixa todos os movimentos aproximadamente 20% mais rápidos." },
            { "Y / X Ratio", "Altera a sensibilidade vertical em relação à horizontal, igualmente para cima e para baixo. 1,05 deixa a saída vertical aproximadamente 5% mais rápida." },
            { "Rotation", "Gira os eixos de referência usados nos ajustes horizontais, verticais e direcionais. Mantenha em 0, exceto se o movimento natural da sua mão for inclinado." },
            { "Curve / Profile", "Seleciona a curva matemática de aceleração. Natural é suave e progressiva; os outros modos produzem formatos diferentes e podem interpretar os parâmetros de outra maneira." },
            { "Gain", "Quando ativado, o formato selecionado é aplicado como ganho — a taxa de mudança da velocidade de saída — em vez de ser aplicado diretamente como sensibilidade. Isso pode mudar bastante a sensação da mesma curva." },
            { "Decay Rate", "Controla a rapidez com que a curva Natural sobe em direção ao limite após o início da aceleração. Valores maiores tornam a transição mais rápida e agressiva." },
            { "Input Offset", "Define a velocidade de entrada necessária para a aceleração começar. Abaixo dessa velocidade, o movimento permanece próximo da sensibilidade base." },
            { "Limit", "Define a força máxima ou o teto aproximado pela curva de aceleração. Valores maiores permitem uma diferença maior entre movimentos lentos e rápidos." },
            { "Vertical Activation", "Muda a rapidez com que o movimento vertical avança pela curva de aceleração. Acima de 1,00 faz a aceleração vertical começar antes; abaixo de 1,00 faz começar depois. Afeta igualmente cima e baixo." },
            { "Vertical Accel Strength", "Multiplica somente a parte acelerada do movimento vertical. Acima de 1,00 adiciona mais aceleração aos movimentos verticais rápidos sem aumentar diretamente a sensibilidade vertical base." },
            { "Mouse DPI", "Normaliza a velocidade de entrada usando o DPI físico do mouse. Informe o DPI real para tornar os perfis mais consistentes entre mouses; use 0 para desativar a normalização." },
            { "Polling Rate (Hz)", "Define a taxa esperada de envio de dados do mouse. Mantenha em 0 para detecção automática. Use um valor manual somente se a temporização automática causar travamentos ou aceleração inconsistente." },
            { "Use a constant interval based on polling rate", "Força os cálculos a usarem um intervalo fixo derivado da taxa de polling configurada. Exige uma taxa manual e serve principalmente para solucionar problemas." },
            { "Disable acceleration for default devices", "Impede a aceleração em dispositivos que não possuem um perfil específico. Ative somente se quiser usar o Raw Accel exclusivamente nos dispositivos configurados." },
            { "Test Driver Communication", "Verifica se o aplicativo consegue ler a configuração ativa do driver Raw Accel. Esse teste não altera o perfil." },
            { "Apply Current Profile", "Envia novamente o perfil atual do settings.json para o driver. Use após edições externas ou quando o driver estiver utilizando uma configuração antiga." },
            { "Disable Acceleration", "Desativa imediatamente a aceleração no driver. O perfil salvo continua disponível e pode ser aplicado novamente depois." },
            { "Left / Right Ratio", "Altera a sensibilidade para a esquerda em relação à direita. 1,00 mantém os dois lados iguais; 1,05 deixa o movimento para a esquerda aproximadamente 5% mais rápido." },
            { "Up / Down Ratio", "Altera a sensibilidade para cima em relação à sensibilidade para baixo. 1,00 mantém as duas direções iguais; 1,05 deixa o movimento para cima aproximadamente 5% mais rápido." },
            { "Angle Snapping (degrees)", "Endireita movimentos próximos dos eixos horizontal ou vertical. 0 desativa; 1–3 produz uma correção leve; valores altos corrigem fortemente o movimento e podem prejudicar a mira livre." }
        };

        private const int WM_INPUT = 0x00FF;
        private const uint RID_INPUT = 0x10000003;
        private const uint RIM_TYPEMOUSE = 0;
        private const uint RIDEV_INPUTSINK = 0x00000100;

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        private sealed class DetectedMouseDevice
        {
            public string DisplayName;
            public string HardwareId;
            public string DevicePath;
            public bool HasSpecificConfiguration;
            public string ConfiguredProfile;
            public bool IsIgnored;
        }

        private sealed class DeviceAssociationContext
        {
            public DetectedMouseDevice Device;
            public ComboBox ProfileSelector;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices([In] RAWINPUTDEVICE[] devices, uint count, uint size);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr rawInput, uint command, IntPtr data, ref uint size, uint headerSize);

        public MainWindow()
        {
            InitializeComponent();
            InitializeTrayIcon();
            rootDirectory = FindRootDirectory();
            settingsPath = IOPath.Combine(rootDirectory, "settings.json");
            Loaded += MainWindow_Loaded;
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new WinForms.NotifyIcon();
            try
            {
                trayIcon.Icon = Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            }
            catch
            {
                trayIcon.Icon = Drawing.SystemIcons.Application;
            }
            trayIcon.Text = "Raw Accel Reimagined";
            trayIcon.Visible = true;

            WinForms.ContextMenuStrip menu = new WinForms.ContextMenuStrip();
            trayOpenItem = new WinForms.ToolStripMenuItem("Open");
            trayOpenItem.Font = new Drawing.Font(trayOpenItem.Font, Drawing.FontStyle.Bold);
            trayOpenItem.Click += delegate { Dispatcher.BeginInvoke(new Action(ShowFromTray)); };
            trayExitItem = new WinForms.ToolStripMenuItem("Close (disable acceleration)");
            trayExitItem.Click += delegate { Dispatcher.BeginInvoke(new Action(ExitFromTray)); };
            menu.Items.Add(trayOpenItem);
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add(trayExitItem);
            trayIcon.ContextMenuStrip = menu;
            trayIcon.MouseDoubleClick += delegate(object sender, WinForms.MouseEventArgs args)
            {
                if (args.Button == WinForms.MouseButtons.Left)
                    Dispatcher.BeginInvoke(new Action(ShowFromTray));
            };
        }

        private string FindRootDirectory()
        {
            DirectoryInfo current = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(IOPath.Combine(current.FullName, "settings.json")) &&
                    File.Exists(IOPath.Combine(current.FullName, "writer.exe")))
                    return current.FullName;
                current = current.Parent;
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        private string T(string english)
        {
            if (String.IsNullOrEmpty(english)) return english;
            string trimmed = english.Trim();
            string translated;
            if (currentLanguage == "pt-BR")
            {
                if (PortugueseText.TryGetValue(trimmed, out translated))
                    return english.Substring(0, english.IndexOf(trimmed, StringComparison.Ordinal)) + translated;
                foreach (KeyValuePair<string, string> item in PortugueseText)
                    if (trimmed.EndsWith(item.Key, StringComparison.Ordinal))
                        return english.Substring(0, english.Length - item.Key.Length) + item.Value;
            }
            else
            {
                foreach (KeyValuePair<string, string> item in PortugueseText)
                {
                    if (trimmed == item.Value)
                        return english.Substring(0, english.IndexOf(trimmed, StringComparison.Ordinal)) + item.Key;
                    if (trimmed.EndsWith(item.Value, StringComparison.Ordinal))
                        return english.Substring(0, english.Length - item.Value.Length) + item.Key;
                }
            }
            return english;
        }

        private void HelpInfo_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string key = button == null ? String.Empty : Convert.ToString(button.Tag);
            string message;
            Dictionary<string, string> catalog = currentLanguage == "pt-BR" ? HelpTextPortuguese : HelpTextEnglish;
            if (String.IsNullOrEmpty(key) || !catalog.TryGetValue(key, out message))
                message = currentLanguage == "pt-BR" ? "Nenhuma explicação está disponível para esta função." : "No explanation is available for this function.";
            string title = (currentLanguage == "pt-BR" ? "Sobre " : "About ") + T(key);
            MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TranslateVisualTree(DependencyObject element)
        {
            if (element == null) return;
            TextBlock textBlock = element as TextBlock;
            if (textBlock != null) textBlock.Text = T(textBlock.Text);
            ContentControl contentControl = element as ContentControl;
            if (contentControl != null && contentControl.Content is string)
                contentControl.Content = T((string)contentControl.Content);
            foreach (object child in LogicalTreeHelper.GetChildren(element))
            {
                DependencyObject dependencyChild = child as DependencyObject;
                if (dependencyChild != null) TranslateVisualTree(dependencyChild);
            }
        }

        private void ApplyLanguage()
        {
            TranslateVisualTree(this);
            UpdateLastMoveText();
            if (trayOpenItem != null) trayOpenItem.Text = currentLanguage == "pt-BR" ? "Abrir" : "Open";
            if (trayExitItem != null) trayExitItem.Text = currentLanguage == "pt-BR" ? "Fechar (desativar aceleração)" : "Close (disable acceleration)";
            DrawChart();
            UpdateManagedProfileSource();
            if (IsLoaded && settings != null && DetectedDevicesPanel != null) RefreshConnectedDevices();
        }

        private void InitializeLanguageSelector()
        {
            changingLanguage = true;
            try
            {
                for (int i = 0; i < LanguageBox.Items.Count; i++)
                {
                    ComboBoxItem item = LanguageBox.Items[i] as ComboBoxItem;
                    if (item != null && String.Equals(Convert.ToString(item.Tag), currentLanguage, StringComparison.OrdinalIgnoreCase))
                    {
                        LanguageBox.SelectedIndex = i;
                        break;
                    }
                }
                if (LanguageBox.SelectedIndex < 0) LanguageBox.SelectedIndex = 0;
            }
            finally
            {
                changingLanguage = false;
            }
            ApplyLanguage();
        }

        private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (changingLanguage || LanguageBox.SelectedItem == null) return;
            ComboBoxItem item = LanguageBox.SelectedItem as ComboBoxItem;
            if (item == null) return;
            string language = Convert.ToString(item.Tag);
            if (String.IsNullOrEmpty(language) || language == currentLanguage) return;
            currentLanguage = language;
            string path = IOPath.Combine(rootDirectory, ".config");
            JObject gui = File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
            gui["Language"] = currentLanguage;
            File.WriteAllText(path, gui.ToString(Formatting.None));
            ApplyLanguage();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadGuiPreferences();
                LoadSettings(null);
                LoadAdvancedSettings();
                DriverStatus.Text = T("Ready");
                InputRateText.Text = " " + pollRate.ToString(CultureInfo.InvariantCulture) + " Hz";
                OutputRateText.Text = " " + pollRate.ToString(CultureInfo.InvariantCulture) + " Hz";
                InitializeLanguageSelector();
                InitializeRawInput();
            }
            catch (Exception ex)
            {
                DriverStatus.Text = T("Configuration error");
                DriverStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 59, 78));
                MessageBox.Show(T("Unable to load the Raw Accel configuration.") + "\n\n" + ex.Message,
                    "Raw Accel Reimagined", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!allowApplicationExit)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (windowSource != null) windowSource.RemoveHook(WindowMessageHook);
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                if (trayIcon.ContextMenuStrip != null) trayIcon.ContextMenuStrip.Dispose();
                trayIcon.Dispose();
            }
            base.OnClosed(e);
        }

        private void HideToTray()
        {
            if (WindowState != WindowState.Minimized) stateBeforeTray = WindowState;
            ShowInTaskbar = false;
            Hide();
            if (!trayMessageShown && trayIcon != null)
            {
                trayMessageShown = true;
                trayIcon.BalloonTipTitle = T("Raw Accel is still active");
                trayIcon.BalloonTipText = T("Double-click to open or right-click to close.");
                trayIcon.BalloonTipIcon = WinForms.ToolTipIcon.Info;
                trayIcon.ShowBalloonTip(3500);
            }
        }

        private void ShowFromTray()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = stateBeforeTray == WindowState.Minimized ? WindowState.Normal : stateBeforeTray;
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }

        private void ExitFromTray()
        {
            try
            {
                if (trayIcon != null) trayIcon.Text = currentLanguage == "pt-BR" ? "Raw Accel - desativando..." : "Raw Accel - disabling...";
                DriverConfig.Deactivate();
                allowApplicationExit = true;
                if (trayIcon != null) trayIcon.Visible = false;
                Close();
            }
            catch (Exception ex)
            {
                if (trayIcon != null) trayIcon.Text = "Raw Accel Reimagined";
                ShowFromTray();
                MessageBox.Show(T("The program was not closed because it could not disable acceleration in the driver.") + "\n\n" + ex.Message,
                    "Raw Accel Reimagined", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeRawInput()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            windowSource = HwndSource.FromHwnd(handle);
            if (windowSource == null) throw new InvalidOperationException(T("Unable to connect the window to Raw Input."));
            windowSource.AddHook(WindowMessageHook);
            RAWINPUTDEVICE[] devices = new RAWINPUTDEVICE[1];
            devices[0].usUsagePage = 0x01;
            devices[0].usUsage = 0x02;
            devices[0].dwFlags = RIDEV_INPUTSINK;
            devices[0].hwndTarget = handle;
            if (!RegisterRawInputDevices(devices, 1, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
                throw new InvalidOperationException(T("Windows rejected Raw Input registration."));
        }

        private IntPtr WindowMessageHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == WM_INPUT) ProcessRawMouseInput(lParam);
            return IntPtr.Zero;
        }

        private void ProcessRawMouseInput(IntPtr rawInputHandle)
        {
            uint size = 0;
            uint headerSize = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));
            if (GetRawInputData(rawInputHandle, RID_INPUT, IntPtr.Zero, ref size, headerSize) == UInt32.MaxValue || size < headerSize + 24) return;
            byte[] buffer = new byte[size];
            GCHandle pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                if (GetRawInputData(rawInputHandle, RID_INPUT, pinned.AddrOfPinnedObject(), ref size, headerSize) == UInt32.MaxValue) return;
                RAWINPUTHEADER header = (RAWINPUTHEADER)Marshal.PtrToStructure(pinned.AddrOfPinnedObject(), typeof(RAWINPUTHEADER));
                if (header.dwType != RIM_TYPEMOUSE) return;
                int offset = (int)headerSize;
                int dx = BitConverter.ToInt32(buffer, offset + 12);
                int dy = BitConverter.ToInt32(buffer, offset + 16);
                uint extra = BitConverter.ToUInt32(buffer, offset + 20);
                if (dx == 0 && dy == 0) return;
                HandleMousePacket(dx, dy, extra);
            }
            finally
            {
                pinned.Free();
            }
        }

        private void HandleMousePacket(int dx, int dy, uint extraInformation)
        {
            long now = Stopwatch.GetTimestamp();
            if (lastRawTimestamp == 0)
            {
                lastRawTimestamp = now;
                return;
            }
            double elapsedMs = (now - lastRawTimestamp) * 1000.0 / Stopwatch.Frequency;
            lastRawTimestamp = now;
            if (elapsedMs <= 0 || elapsedMs > 100) elapsedMs = 1000.0 / Math.Max(125, pollRate);

            double packetOutputSpeed = Math.Sqrt((double)dx * dx + (double)dy * dy) / elapsedMs;
            if (!hasSmoothedSpeed)
            {
                smoothedOutputSpeed = packetOutputSpeed;
                hasSmoothedSpeed = true;
            }
            else smoothedOutputSpeed = smoothedOutputSpeed * 0.72 + packetOutputSpeed * 0.28;

            double directInputSpeed = 0;
            double directRatio = 0;
            bool hasOriginalPacket = TryReadOriginalPacket(extraInformation, elapsedMs, packetOutputSpeed, out directInputSpeed, out directRatio);
            if (hasOriginalPacket)
            {
                smoothedInputSpeed = smoothedInputSpeed <= 0 ? directInputSpeed : smoothedInputSpeed * 0.72 + directInputSpeed * 0.28;
            }

            lastMouseDx = dx;
            lastMouseDy = dy;
            UpdateLastMoveText();
            if ((now - lastMarkerTimestamp) * 1000.0 / Stopwatch.Frequency < 14) return;
            lastMarkerTimestamp = now;

            if (hasOriginalPacket)
            {
                lastMarkerInputSpeed = smoothedInputSpeed;
                lastMarkerRatio = CurveRatioAt(lastMarkerInputSpeed);
            }
            else
            {
                FindCurvePositionFromOutput(smoothedOutputSpeed, out lastMarkerInputSpeed, out lastMarkerRatio);
            }
            hasMouseMarker = true;
            PositionMouseMarker();
        }

        private void UpdateLastMoveText()
        {
            string label = currentLanguage == "pt-BR" ? "Último" : "Last";
            LastMoveText.Text = label + " (x, y): (" + lastMouseDx.ToString(CultureInfo.InvariantCulture) + ", " + lastMouseDy.ToString(CultureInfo.InvariantCulture) + ")";
        }

        private bool TryReadOriginalPacket(uint extra, double elapsedMs, double outputSpeed, out double inputSpeed, out double ratio)
        {
            inputSpeed = 0;
            ratio = 0;
            if (extra == 0 || displayedCurve.Count < 2) return false;
            short originalX = unchecked((short)(extra & 0xFFFF));
            short originalY = unchecked((short)((extra >> 16) & 0xFFFF));
            double magnitude = Math.Sqrt((double)originalX * originalX + (double)originalY * originalY);
            if (magnitude < 0.5) return false;
            inputSpeed = magnitude / elapsedMs;
            if (inputSpeed <= 0 || inputSpeed > 160) return false;
            ratio = outputSpeed / inputSpeed;
            double expected = CurveRatioAt(inputSpeed);
            return ratio > 0 && Math.Abs(ratio - expected) <= Math.Max(0.75, expected * 0.55);
        }

        private void FindCurvePositionFromOutput(double outputSpeed, out double inputSpeed, out double ratio)
        {
            inputSpeed = 0;
            ratio = displayedCurve.Count == 0 ? 1 : displayedCurve[0];
            if (displayedCurve.Count < 2) return;
            double bestDifference = Double.MaxValue;
            for (int i = 0; i < displayedCurve.Count; i++)
            {
                double candidateInput = 40.0 * i / (displayedCurve.Count - 1.0);
                double candidateOutput = candidateInput * displayedCurve[i];
                double difference = Math.Abs(candidateOutput - outputSpeed);
                if (difference < bestDifference)
                {
                    bestDifference = difference;
                    inputSpeed = candidateInput;
                    ratio = displayedCurve[i];
                }
            }
        }

        private double CurveRatioAt(double inputSpeed)
        {
            if (displayedCurve.Count == 0) return 1;
            double position = Math.Max(0, Math.Min(40, inputSpeed)) / 40.0 * (displayedCurve.Count - 1);
            int lower = (int)Math.Floor(position);
            int upper = Math.Min(displayedCurve.Count - 1, lower + 1);
            double fraction = position - lower;
            return displayedCurve[lower] * (1 - fraction) + displayedCurve[upper] * fraction;
        }

        private void LoadGuiPreferences()
        {
            string path = IOPath.Combine(rootDirectory, ".config");
            if (!File.Exists(path)) return;
            JObject gui = JObject.Parse(File.ReadAllText(path));
            if (gui["DPI"] != null) displayDpi = gui["DPI"].Value<int>();
            if (gui["PollRate"] != null) pollRate = gui["PollRate"].Value<int>();
            if (gui["Language"] != null && gui["Language"].ToString() == "pt-BR") currentLanguage = "pt-BR";
            ignoredDeviceIds.Clear();
            JArray ignored = gui["IgnoredDeviceIds"] as JArray;
            if (ignored != null)
                foreach (JToken id in ignored)
                    if (!String.IsNullOrWhiteSpace(id.ToString())) ignoredDeviceIds.Add(id.ToString());
        }

        private void LoadSettings(string profileToSelect)
        {
            loading = true;
            try
            {
                settings = JObject.Parse(File.ReadAllText(settingsPath));
                JArray profiles = settings["profiles"] as JArray;
                if (profiles == null || profiles.Count == 0) throw new InvalidDataException(T("No profiles were found in settings.json."));

                ProfileBox.Items.Clear();
                int selected = 0;
                for (int i = 0; i < profiles.Count; i++)
                {
                    string name = profiles[i]["name"] == null ? "Profile " + (i + 1) : profiles[i]["name"].ToString();
                    ProfileBox.Items.Add(name);
                    if (!String.IsNullOrEmpty(profileToSelect) && name == profileToSelect) selected = i;
                }
                ProfileBox.SelectedIndex = selected;
                LoadProfile(selected);
            }
            finally
            {
                loading = false;
            }
        }

        private void LoadProfile(int index)
        {
            JArray profiles = settings["profiles"] as JArray;
            if (profiles == null || index < 0 || index >= profiles.Count) return;
            JObject profile = (JObject)profiles[index];
            JObject accel = (JObject)profile["Whole or horizontal accel parameters"];
            JObject domain = (JObject)profile["Stretches domain for horizontal vs vertical inputs"];
            JObject range = (JObject)profile["Stretches accel range for horizontal vs vertical inputs"];

            double sens = GetDouble(profile, "Output DPI", 1000) / 1000.0;
            double ratio = GetDouble(profile, "Y/X output DPI ratio (vertical sens multiplier)", 1);
            double rotation = GetDouble(profile, "Degrees of rotation", 0);
            double decay = GetDouble(accel, "decayRate", 1);
            double offset = GetDouble(accel, "inputOffset", 0);
            double limit = GetDouble(accel, "limit", 0);
            double anisotropy = domain == null ? 1 : GetDouble(domain, "y", 1);
            double verticalRange = range == null ? 1 : GetDouble(range, "y", 1);

            SensBox.Text = FormatNumber(sens);
            RatioBox.Text = FormatNumber(ratio);
            RotationBox.Text = FormatNumber(rotation);
            DecayBox.Text = FormatNumber(decay);
            OffsetBox.Text = FormatNumber(offset);
            LimitBox.Text = FormatNumber(limit);
            AnisotropyBox.Text = FormatNumber(anisotropy);
            VerticalRangeBox.Text = FormatNumber(verticalRange);
            SensCurrent.Text = FormatNumber(sens);
            RatioCurrent.Text = FormatNumber(ratio);
            RotationCurrent.Text = FormatNumber(rotation);
            DecayCurrent.Text = FormatNumber(decay);
            OffsetCurrent.Text = FormatNumber(offset);
            LimitCurrent.Text = FormatNumber(limit);
            GainToggle.IsChecked = accel != null && accel["Gain / Velocity"] != null && accel["Gain / Velocity"].Value<bool>();

            string mode = accel == null || accel["mode"] == null ? "natural" : accel["mode"].ToString();
            SelectMode(mode);
            StatRatio.Text = ratio.ToString("0.00", CultureInfo.InvariantCulture);
            StatDpi.Text = displayDpi.ToString(CultureInfo.InvariantCulture);
            UpdateManagedProfileSource();
            DrawChart();
        }

        private void UpdateManagedProfileSource()
        {
            if (ManagedProfileSourceText == null) return;
            string selected = ProfileBox == null || ProfileBox.SelectedItem == null ? "—" : ProfileBox.SelectedItem.ToString();
            ManagedProfileSourceText.Text = T("Selected profile") + ": " + selected;
        }

        private static double GetDouble(JObject obj, string key, double fallback)
        {
            if (obj == null || obj[key] == null) return fallback;
            try
            {
                return obj[key].Value<double>();
            }
            catch
            {
                double result;
                string text = obj[key].ToString().Trim().Replace(',', '.');
                return Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ? result : fallback;
            }
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private void SelectMode(string mode)
        {
            for (int i = 0; i < ModeBox.Items.Count; i++)
            {
                ComboBoxItem item = ModeBox.Items[i] as ComboBoxItem;
                if (item != null && String.Equals(Convert.ToString(item.Tag), mode, StringComparison.OrdinalIgnoreCase))
                {
                    ModeBox.SelectedIndex = i;
                    return;
                }
            }
            ModeBox.SelectedIndex = 0;
        }

        private string SelectedMode()
        {
            ComboBoxItem item = ModeBox.SelectedItem as ComboBoxItem;
            if (item == null) return "natural";
            string value = Convert.ToString(item.Tag);
            return String.IsNullOrEmpty(value) ? "natural" : value;
        }

        private double ReadNumber(TextBox box, string label)
        {
            double value;
            string text = box.Text.Trim().Replace(',', '.');
            if (!Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || Double.IsNaN(value) || Double.IsInfinity(value))
                throw new InvalidDataException(String.Format(T("Invalid value in {0}."), T(label)));
            return value;
        }

        private void NumericBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox box = sender as TextBox;
            if (box != null) box.SelectAll();
        }

        private void NumericBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBox box = sender as TextBox;
            if (box != null && !box.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                box.Focus();
            }
        }

        private void NumericBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox box = sender as TextBox;
            if (box == null) return;
            if (e.Text == "." || e.Text == ",")
            {
                InsertDecimalSeparator(box);
                e.Handled = true;
                return;
            }
            foreach (char character in e.Text)
            {
                if (!Char.IsDigit(character) && character != '-')
                {
                    e.Handled = true;
                    return;
                }
            }
            if (e.Text == "-" && (box.SelectionStart != 0 || box.Text.Contains("-"))) e.Handled = true;
        }

        private void NumericBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox box = sender as TextBox;
            if (box != null && e.Key == Key.Decimal)
            {
                InsertDecimalSeparator(box);
                e.Handled = true;
            }
        }

        private static void InsertDecimalSeparator(TextBox box)
        {
            int start = box.SelectionStart;
            string text = box.Text ?? String.Empty;
            if (box.SelectionLength > 0) text = text.Remove(start, box.SelectionLength);
            text = text.Replace(',', '.');
            int existing = text.IndexOf('.');
            if (existing >= 0)
            {
                box.Text = text;
                box.CaretIndex = existing + 1;
                return;
            }
            if (text.Length == 0 || text == "-")
            {
                text += "0";
                start = text.Length;
            }
            box.Text = text.Insert(start, ".");
            box.CaretIndex = start + 1;
        }

        private void NumericBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox box = sender as TextBox;
            if (box == null) return;
            double value;
            if (Double.TryParse((box.Text ?? String.Empty).Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                box.Text = (box == DeviceDpiBox || box == DevicePollRateBox) ? value.ToString("0", CultureInfo.InvariantCulture) : FormatNumber(value);
        }

        private void Charts_Click(object sender, RoutedEventArgs e)
        {
            ChartsPage.Visibility = Visibility.Visible;
            AdvancedPage.Visibility = Visibility.Collapsed;
            ChartsNavText.Foreground = new SolidColorBrush(Color.FromRgb(21, 148, 251));
            ChartsNavIcon.Foreground = ChartsNavText.Foreground;
            AdvancedNavText.Foreground = new SolidColorBrush(Color.FromRgb(148, 162, 184));
            AdvancedNavIcon.Foreground = AdvancedNavText.Foreground;
            NavUnderline.RenderTransform = new TranslateTransform(-62, 0);
        }

        private void Advanced_Click(object sender, RoutedEventArgs e)
        {
            ChartsPage.Visibility = Visibility.Collapsed;
            AdvancedPage.Visibility = Visibility.Visible;
            AdvancedNavText.Foreground = new SolidColorBrush(Color.FromRgb(21, 148, 251));
            AdvancedNavIcon.Foreground = AdvancedNavText.Foreground;
            ChartsNavText.Foreground = new SolidColorBrush(Color.FromRgb(148, 162, 184));
            ChartsNavIcon.Foreground = ChartsNavText.Foreground;
            NavUnderline.RenderTransform = new TranslateTransform(62, 0);
            LoadAdvancedSettings();
            RefreshConnectedDevices();
        }

        private void RefreshDevices_Click(object sender, RoutedEventArgs e)
        {
            RefreshConnectedDevices();
        }

        private void ShowIgnoredDevicesCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (settings != null) RefreshConnectedDevices();
        }

        private void SaveIgnoredDevicePreferences()
        {
            string path = IOPath.Combine(rootDirectory, ".config");
            JObject gui = File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();
            gui["IgnoredDeviceIds"] = new JArray(ignoredDeviceIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase));
            File.WriteAllText(path, gui.ToString(Formatting.None));
        }

        private void RefreshConnectedDevices()
        {
            if (DetectedDevicesPanel == null || DeviceScanStatusText == null) return;
            DetectedDevicesPanel.Children.Clear();
            try
            {
                List<DetectedMouseDevice> allDevices = EnumerateMouseDevices();
                int ignoredCount = allDevices.Count(device => device.IsIgnored);
                bool showIgnored = ShowIgnoredDevicesCheck != null && ShowIgnoredDevicesCheck.IsChecked == true;
                List<DetectedMouseDevice> devices = showIgnored ? allDevices : allDevices.Where(device => !device.IsIgnored).ToList();
                DeviceScanStatusText.Text = currentLanguage == "pt-BR"
                    ? allDevices.Count.ToString(CultureInfo.InvariantCulture) + " interface(s) com capacidade de mouse • " + ignoredCount.ToString(CultureInfo.InvariantCulture) + " ignorada(s)"
                    : allDevices.Count.ToString(CultureInfo.InvariantCulture) + " mouse-capable interface(s) • " + ignoredCount.ToString(CultureInfo.InvariantCulture) + " ignored";
                DeviceScanStatusText.Foreground = new SolidColorBrush(allDevices.Count == 0 ? Color.FromRgb(255, 188, 82) : Color.FromRgb(32, 197, 107));

                if (devices.Count == 0)
                {
                    Border empty = CreateDeviceCard();
                    empty.Child = new TextBlock
                    {
                        Text = allDevices.Count == 0 ? T("No mouse devices were detected.") : T("All detected devices are ignored."),
                        Foreground = new SolidColorBrush(Color.FromRgb(147, 162, 184))
                    };
                    DetectedDevicesPanel.Children.Add(empty);
                    return;
                }

                for (int i = 0; i < devices.Count; i++)
                    DetectedDevicesPanel.Children.Add(CreateDeviceCard(devices[i]));
            }
            catch (Exception ex)
            {
                DeviceScanStatusText.Text = T("Detection failed") + ": " + ex.Message;
                DeviceScanStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 91, 106));
            }
        }

        private List<DetectedMouseDevice> EnumerateMouseDevices()
        {
            List<DetectedMouseDevice> result = new List<DetectedMouseDevice>();
            IList<MultiHandleDevice> originalDevices = MultiHandleDevice.GetList();
            foreach (MultiHandleDevice originalDevice in originalDevices)
            {
                if (originalDevice == null || String.IsNullOrWhiteSpace(originalDevice.id)) continue;
                JObject configured = FindDeviceSettings(settings, originalDevice.id);
                result.Add(new DetectedMouseDevice
                {
                    DevicePath = originalDevice.id.Trim(),
                    HardwareId = ExtractHardwareId(originalDevice.id),
                    DisplayName = String.IsNullOrWhiteSpace(originalDevice.name) ? originalDevice.id.Trim() : originalDevice.name.Trim(),
                    HasSpecificConfiguration = configured != null,
                    ConfiguredProfile = configured == null || configured["profile"] == null ? null : configured["profile"].ToString(),
                    IsIgnored = ignoredDeviceIds.Contains(originalDevice.id.Trim())
                });
            }
            return result.OrderBy(device => device.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        private static JObject FindDeviceSettings(JObject source, string id)
        {
            JArray configuredDevices = source == null ? null : source["devices"] as JArray;
            if (configuredDevices == null || String.IsNullOrWhiteSpace(id)) return null;
            return configuredDevices.OfType<JObject>().FirstOrDefault(item =>
                item["id"] != null && String.Equals(item["id"].ToString(), id, StringComparison.OrdinalIgnoreCase));
        }

        private static string ExtractHardwareId(string devicePath)
        {
            string upper = (devicePath ?? String.Empty).ToUpperInvariant();
            int vid = upper.IndexOf("VID_", StringComparison.Ordinal);
            int pid = upper.IndexOf("PID_", StringComparison.Ordinal);
            if (vid >= 0 && vid + 8 <= upper.Length && pid >= 0 && pid + 8 <= upper.Length)
                return upper.Substring(vid, 8) + " / " + upper.Substring(pid, 8);
            if (String.IsNullOrWhiteSpace(upper)) return "RAW INPUT";
            int separator = upper.IndexOf('\\');
            return separator > 0 ? upper.Substring(0, separator) : upper;
        }

        private static Border CreateDeviceCard()
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(11, 23, 40)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(32, 51, 79)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 9)
            };
        }

        private Border CreateDeviceCard(DetectedMouseDevice device)
        {
            Border card = CreateDeviceCard();
            card.ToolTip = device.DevicePath;
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(290) });

            Border icon = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                Background = new SolidColorBrush(Color.FromRgb(18, 62, 99)),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text = "●",
                    Foreground = new SolidColorBrush(Color.FromRgb(21, 148, 251)),
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            StackPanel details = new StackPanel();
            details.Children.Add(new TextBlock
            {
                Text = device.DisplayName,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(231, 237, 246))
            });
            details.Children.Add(new TextBlock
            {
                Text = device.HardwareId,
                Foreground = new SolidColorBrush(Color.FromRgb(147, 162, 184)),
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0)
            });
            details.Children.Add(new TextBlock
            {
                Text = T("Device identifier") + ": " + device.DevicePath,
                Foreground = new SolidColorBrush(Color.FromRgb(96, 115, 142)),
                FontSize = 10,
                Margin = new Thickness(0, 3, 8, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = device.DevicePath
            });
            Grid.SetColumn(details, 1);
            grid.Children.Add(details);

            StackPanel status = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            status.Children.Add(new TextBlock
            {
                Text = T("Connected"),
                Foreground = new SolidColorBrush(Color.FromRgb(32, 197, 107)),
                HorizontalAlignment = HorizontalAlignment.Right,
                FontWeight = FontWeights.SemiBold
            });
            status.Children.Add(new TextBlock
            {
                Text = device.IsIgnored ? T("Ignored in this app") : device.HasSpecificConfiguration
                    ? T("Assigned profile") + ": " + device.ConfiguredProfile
                    : T("Uses default settings"),
                Foreground = new SolidColorBrush(device.IsIgnored ? Color.FromRgb(255, 188, 82) : device.HasSpecificConfiguration ? Color.FromRgb(21, 148, 251) : Color.FromRgb(147, 162, 184)),
                HorizontalAlignment = HorizontalAlignment.Right,
                FontSize = 11,
                Margin = new Thickness(0, 3, 0, 0)
            });

            DeviceAssociationContext context = new DeviceAssociationContext { Device = device };
            if (device.IsIgnored)
            {
                card.Opacity = 0.78;
                Button restore = new Button
                {
                    Content = T("Restore Device"),
                    Height = 32,
                    FontSize = 12,
                    Margin = new Thickness(0, 8, 0, 0),
                    Tag = context,
                    Style = (Style)FindResource("ModernButton")
                };
                restore.Click += RestoreIgnoredDevice_Click;
                status.Children.Add(restore);
                Grid.SetColumn(status, 2);
                grid.Children.Add(status);
                card.Child = grid;
                return card;
            }

            ComboBox profileSelector = new ComboBox { Height = 34, Margin = new Thickness(0, 7, 0, 6) };
            JArray profiles = settings == null ? null : settings["profiles"] as JArray;
            if (profiles != null)
            {
                foreach (JObject profile in profiles.OfType<JObject>())
                    if (profile["name"] != null) profileSelector.Items.Add(profile["name"].ToString());
            }
            string preferredProfile = device.HasSpecificConfiguration ? device.ConfiguredProfile :
                (ProfileBox.SelectedItem == null ? null : ProfileBox.SelectedItem.ToString());
            profileSelector.SelectedIndex = Math.Max(0, profileSelector.Items.IndexOf(preferredProfile));
            status.Children.Add(profileSelector);

            context.ProfileSelector = profileSelector;
            Grid actionRow = new Grid();
            actionRow.ColumnDefinitions.Add(new ColumnDefinition());
            actionRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
            actionRow.ColumnDefinitions.Add(new ColumnDefinition());
            Button associate = new Button
            {
                Content = T("Associate Profile"),
                Height = 32,
                FontSize = 12,
                Tag = context,
                Style = (Style)FindResource("AccentButton")
            };
            associate.Click += AssociateDeviceProfile_Click;
            actionRow.Children.Add(associate);
            Button secondary = new Button
            {
                Content = T(device.HasSpecificConfiguration ? "Use Default" : "Not a Mouse"),
                Height = 32,
                FontSize = 12,
                Tag = context,
                Style = (Style)FindResource("ModernButton")
            };
            if (device.HasSpecificConfiguration) secondary.Click += UseDefaultDeviceSettings_Click;
            else secondary.Click += IgnoreDevice_Click;
            Grid.SetColumn(secondary, 2);
            actionRow.Children.Add(secondary);
            status.Children.Add(actionRow);
            Grid.SetColumn(status, 2);
            grid.Children.Add(status);
            card.Child = grid;
            return card;
        }

        private void AssociateDeviceProfile_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            DeviceAssociationContext context = button == null ? null : button.Tag as DeviceAssociationContext;
            if (context == null || context.Device == null || context.ProfileSelector == null || context.ProfileSelector.SelectedItem == null) return;
            string profileName = context.ProfileSelector.SelectedItem.ToString();
            string question = currentLanguage == "pt-BR"
                ? "Confirme que \"" + context.Device.DisplayName + "\" é um mouse físico.\n\nAssociar o perfil \"" + profileName + "\" a este dispositivo?"
                : "Confirm that \"" + context.Device.DisplayName + "\" is a physical mouse.\n\nAssociate profile \"" + profileName + "\" with this device?";
            if (MessageBox.Show(this, question, T("Device Association"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            SaveDeviceAssociation(context.Device, profileName, false);
        }

        private void UseDefaultDeviceSettings_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            DeviceAssociationContext context = button == null ? null : button.Tag as DeviceAssociationContext;
            if (context == null || context.Device == null) return;
            string question = currentLanguage == "pt-BR"
                ? "Remover a associação específica de \"" + context.Device.DisplayName + "\" e voltar às configurações padrão?"
                : "Remove the specific association for \"" + context.Device.DisplayName + "\" and return to default settings?";
            if (MessageBox.Show(this, question, T("Device Association"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            SaveDeviceAssociation(context.Device, null, true);
        }

        private void IgnoreDevice_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            DeviceAssociationContext context = button == null ? null : button.Tag as DeviceAssociationContext;
            if (context == null || context.Device == null) return;
            string question = currentLanguage == "pt-BR"
                ? "Marcar \"" + context.Device.DisplayName + "\" como não sendo mouse e ocultar este dispositivo da lista?\n\nIsso altera somente a interface e não modifica o driver."
                : "Mark \"" + context.Device.DisplayName + "\" as not a mouse and hide it from the list?\n\nThis changes only the interface and does not modify the driver.";
            if (MessageBox.Show(this, question, T("Connected Devices"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            ignoredDeviceIds.Add(context.Device.DevicePath);
            SaveIgnoredDevicePreferences();
            RefreshConnectedDevices();
        }

        private void RestoreIgnoredDevice_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            DeviceAssociationContext context = button == null ? null : button.Tag as DeviceAssociationContext;
            if (context == null || context.Device == null) return;
            ignoredDeviceIds.Remove(context.Device.DevicePath);
            SaveIgnoredDevicePreferences();
            RefreshConnectedDevices();
        }

        private void SaveDeviceAssociation(DetectedMouseDevice device, string profileName, bool removeAssociation)
        {
            string backupPath = null;
            try
            {
                JObject edited = JObject.Parse(File.ReadAllText(settingsPath));
                JArray configuredDevices = edited["devices"] as JArray;
                if (configuredDevices == null)
                {
                    configuredDevices = new JArray();
                    edited["devices"] = configuredDevices;
                }

                List<JObject> matches = configuredDevices.OfType<JObject>().Where(item =>
                    item["id"] != null && String.Equals(item["id"].ToString(), device.DevicePath, StringComparison.OrdinalIgnoreCase)).ToList();

                if (removeAssociation)
                {
                    foreach (JObject match in matches) match.Remove();
                    if (matches.Count == 0)
                    {
                        settings = edited;
                        RefreshConnectedDevices();
                        SetAdvancedDriverStatus("Device returned to default settings", true);
                        return;
                    }
                }
                else
                {
                    JArray profiles = edited["profiles"] as JArray;
                    bool profileExists = profiles != null && profiles.OfType<JObject>().Any(profile =>
                        profile["name"] != null && String.Equals(profile["name"].ToString(), profileName, StringComparison.Ordinal));
                    if (!profileExists) throw new InvalidDataException(T("The selected profile was not found."));

                    JObject association = matches.FirstOrDefault();
                    if (association == null)
                    {
                        JObject defaultConfig = edited["defaultDeviceConfig"] as JObject;
                        if (defaultConfig == null) throw new InvalidDataException(T("defaultDeviceConfig was not found."));
                        association = new JObject();
                        association["config"] = defaultConfig.DeepClone();
                        configuredDevices.Add(association);
                    }
                    association["name"] = device.DisplayName;
                    association["profile"] = profileName;
                    association["id"] = device.DevicePath;
                    if (!(association["config"] is JObject))
                    {
                        JObject defaultConfig = edited["defaultDeviceConfig"] as JObject;
                        if (defaultConfig == null) throw new InvalidDataException(T("defaultDeviceConfig was not found."));
                        association["config"] = defaultConfig.DeepClone();
                    }
                    foreach (JObject duplicate in matches.Skip(1)) duplicate.Remove();
                }

                Tuple<DriverConfig, string> validation = DriverConfig.Convert(edited.ToString(Formatting.None));
                if (validation == null || validation.Item1 == null)
                    throw new InvalidDataException(T("The original engine rejected the configuration.") + " " + (validation == null ? String.Empty : validation.Item2));

                string backupDirectory = IOPath.Combine(rootDirectory, "backups", "modern-ui");
                Directory.CreateDirectory(backupDirectory);
                backupPath = IOPath.Combine(backupDirectory, "device-association-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".json");
                File.Copy(settingsPath, backupPath, false);
                File.WriteAllText(settingsPath, edited.ToString(Formatting.Indented));
                int exitCode = RunSettingsWriter();
                if (exitCode != 0) throw new InvalidOperationException("writer.exe retornou o código " + exitCode + ".");

                settings = JObject.Parse(File.ReadAllText(settingsPath));
                RefreshConnectedDevices();
                SetAdvancedDriverStatus(removeAssociation ? "Device returned to default settings" : "Device profile applied", true);
                DriverStatus.Text = T("Active");
                DriverStatus.Foreground = new SolidColorBrush(Color.FromRgb(32, 197, 107));
            }
            catch (Exception ex)
            {
                if (backupPath != null && File.Exists(backupPath)) File.Copy(backupPath, settingsPath, true);
                settings = JObject.Parse(File.ReadAllText(settingsPath));
                RefreshConnectedDevices();
                SetAdvancedDriverStatus("Device association was not applied", false);
                MessageBox.Show(this, ex.Message, T("Device Association"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadAdvancedSettings()
        {
            if (settings == null) return;
            JObject device = settings["defaultDeviceConfig"] as JObject;
            if (device != null)
            {
                DeviceDpiBox.Text = GetDouble(device, "DPI (normalizes input speed unit: counts/ms -> in/s)", 0).ToString("0", CultureInfo.InvariantCulture);
                DevicePollRateBox.Text = GetDouble(device, "Polling rate Hz (keep at 0 for automatic adjustment)", 0).ToString("0", CultureInfo.InvariantCulture);
                ConstantIntervalCheck.IsChecked = device["Use constant time interval based on polling rate"] != null && device["Use constant time interval based on polling rate"].Value<bool>();
                DisableDefaultDeviceCheck.IsChecked = device["disable"] != null && device["disable"].Value<bool>();
            }

            JArray profiles = settings["profiles"] as JArray;
            int index = ProfileBox.SelectedIndex < 0 ? 0 : ProfileBox.SelectedIndex;
            if (profiles == null || index < 0 || index >= profiles.Count) return;
            JObject profile = profiles[index] as JObject;
            if (profile == null) return;
            AxisLeftRightBox.Text = FormatNumber(GetDouble(profile, "L/R output DPI ratio (left sens multiplier)", 1));
            AxisUpDownBox.Text = FormatNumber(GetDouble(profile, "U/D output DPI ratio (up sens multiplier)", 1));
            AxisSnapBox.Text = FormatNumber(GetDouble(profile, "Degrees of angle snapping", 0));
            JObject speedCalculation = profile["Input speed calculation parameters"] as JObject;
            InputSmoothingBox.Text = FormatNumber(GetDouble(speedCalculation, "Time in ms after which an input is weighted at half its original value.", 0));
            SensitivitySmoothingBox.Text = FormatNumber(GetDouble(speedCalculation, "Time in ms after which scale is weighted at half its original value.", 0));
            OutputSmoothingBox.Text = FormatNumber(GetDouble(speedCalculation, "Time in ms after which an output is weighted at half its original value.", 0));
            InputSpeedCapBox.Text = FormatNumber(GetDouble(profile, "Input Speed Cap", 0));
        }

        private int ReadWholeNumber(TextBox box, string label)
        {
            double value = ReadNumber(box, label);
            if (Math.Abs(value - Math.Round(value)) > 0.000001)
                throw new InvalidDataException(String.Format(T("{0} must be an integer."), T(label)));
            if (value < Int32.MinValue || value > Int32.MaxValue)
                throw new InvalidDataException(String.Format(T("{0} is outside the allowed range."), T(label)));
            return (int)Math.Round(value);
        }

        private int RunSettingsWriter()
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = IOPath.Combine(rootDirectory, "writer.exe");
            start.Arguments = "\"" + settingsPath + "\"";
            start.WorkingDirectory = rootDirectory;
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            using (Process process = Process.Start(start))
            {
                if (process == null) throw new InvalidOperationException(T("Could not start writer.exe."));
                if (!process.WaitForExit(7000)) throw new TimeoutException(T("The driver did not respond within the expected time."));
                return process.ExitCode;
            }
        }

        private void SetAdvancedDriverStatus(string text, bool success)
        {
            Color color = success ? Color.FromRgb(32, 197, 107) : Color.FromRgb(255, 91, 106);
            AdvancedDriverStatus.Text = T(text);
            AdvancedDriverStatus.Foreground = new SolidColorBrush(color);
            AdvancedStatusDot.Fill = new SolidColorBrush(color);
        }

        private void CreateCleanProfile_Click(object sender, RoutedEventArgs e)
        {
            CreateOrDuplicateProfile(false);
        }

        private void DuplicateProfile_Click(object sender, RoutedEventArgs e)
        {
            CreateOrDuplicateProfile(true);
        }

        private void CreateOrDuplicateProfile(bool duplicateSelected)
        {
            string backupPath = null;
            string previousProfile = ProfileBox.SelectedItem == null ? null : ProfileBox.SelectedItem.ToString();
            try
            {
                string newName = ValidateNewProfileName(NewProfileNameBox.Text);
                JObject edited = JObject.Parse(File.ReadAllText(settingsPath));
                JArray profiles = edited["profiles"] as JArray;
                if (profiles == null || profiles.Count == 0) throw new InvalidDataException(T("No profiles were found in settings.json."));
                if (profiles.OfType<JObject>().Any(profile => profile["name"] != null && String.Equals(profile["name"].ToString(), newName, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException(T("A profile with this name already exists."));

                JObject newProfile;
                if (duplicateSelected)
                {
                    int index = ProfileBox.SelectedIndex < 0 ? 0 : ProfileBox.SelectedIndex;
                    if (index < 0 || index >= profiles.Count) throw new InvalidDataException(T("The selected profile was not found."));
                    newProfile = (JObject)profiles[index].DeepClone();
                }
                else
                {
                    DriverConfig cleanConfig = DriverConfig.GetDefault();
                    JObject cleanJson = cleanConfig == null ? null : cleanConfig.ToJObject();
                    JArray cleanProfiles = cleanJson == null ? null : cleanJson["profiles"] as JArray;
                    if (cleanProfiles == null || cleanProfiles.Count == 0)
                        throw new InvalidDataException(T("The original engine rejected the configuration."));
                    newProfile = (JObject)cleanProfiles[0].DeepClone();
                }
                newProfile["name"] = newName;
                profiles.Add(newProfile);

                Tuple<DriverConfig, string> validation = DriverConfig.Convert(edited.ToString(Formatting.None));
                if (validation == null || validation.Item1 == null)
                    throw new InvalidDataException(T("The original engine rejected the configuration.") + " " + (validation == null ? String.Empty : validation.Item2));

                string backupDirectory = IOPath.Combine(rootDirectory, "backups", "modern-ui");
                Directory.CreateDirectory(backupDirectory);
                backupPath = IOPath.Combine(backupDirectory, "profile-management-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".json");
                File.Copy(settingsPath, backupPath, false);
                File.WriteAllText(settingsPath, edited.ToString(Formatting.Indented));
                int exitCode = RunSettingsWriter();
                if (exitCode != 0) throw new InvalidOperationException("writer.exe retornou o código " + exitCode + ".");

                NewProfileNameBox.Clear();
                LoadSettings(newName);
                LoadAdvancedSettings();
                RefreshConnectedDevices();
                SetAdvancedDriverStatus(duplicateSelected ? "Profile duplicated" : "Profile created", true);
                DriverStatus.Text = T("Active");
                DriverStatus.Foreground = new SolidColorBrush(Color.FromRgb(32, 197, 107));
            }
            catch (Exception ex)
            {
                if (backupPath != null && File.Exists(backupPath)) File.Copy(backupPath, settingsPath, true);
                if (File.Exists(settingsPath)) LoadSettings(previousProfile);
                SetAdvancedDriverStatus("Profile operation failed", false);
                MessageBox.Show(this, ex.Message, T("Profile Management"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private string ValidateNewProfileName(string value)
        {
            string name = (value ?? String.Empty).Trim();
            if (name.Length == 0) throw new InvalidDataException(T("Profile name is required."));
            if (name.Length > 48 || name.Any(character => Char.IsControl(character)) || name.IndexOfAny(new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' }) >= 0)
                throw new InvalidDataException(T("Profile name contains invalid characters."));
            return name;
        }

        private void ApplyDeviceSettings_Click(object sender, RoutedEventArgs e)
        {
            string backupPath = null;
            try
            {
                int dpi = ReadWholeNumber(DeviceDpiBox, "Mouse DPI");
                int pollingRate = ReadWholeNumber(DevicePollRateBox, "Polling Rate");
                if (dpi < 0 || (dpi > 0 && dpi < 50) || dpi > 100000)
                    throw new InvalidDataException(T("Mouse DPI must be 0 or between 50 and 100000."));
                if (pollingRate != 0 && (pollingRate < 125 || pollingRate > 8000))
                    throw new InvalidDataException(T("Polling Rate must be 0 or between 125 and 8000 Hz."));
                if (ConstantIntervalCheck.IsChecked == true && pollingRate == 0)
                    throw new InvalidDataException(T("Enter a Polling Rate to use a constant interval."));

                JObject edited = JObject.Parse(File.ReadAllText(settingsPath));
                JObject device = edited["defaultDeviceConfig"] as JObject;
                if (device == null) throw new InvalidDataException(T("defaultDeviceConfig was not found."));
                device["disable"] = DisableDefaultDeviceCheck.IsChecked == true;
                device["Use constant time interval based on polling rate"] = ConstantIntervalCheck.IsChecked == true;
                device["DPI (normalizes input speed unit: counts/ms -> in/s)"] = dpi;
                device["Polling rate Hz (keep at 0 for automatic adjustment)"] = pollingRate;

                Tuple<DriverConfig, string> validation = DriverConfig.Convert(edited.ToString(Formatting.None));
                if (validation == null || validation.Item1 == null)
                    throw new InvalidDataException(T("The original engine rejected the configuration.") + " " + (validation == null ? String.Empty : validation.Item2));

                string backupDirectory = IOPath.Combine(rootDirectory, "backups", "modern-ui");
                Directory.CreateDirectory(backupDirectory);
                backupPath = IOPath.Combine(backupDirectory, "device-settings-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
                File.Copy(settingsPath, backupPath, false);
                File.WriteAllText(settingsPath, edited.ToString(Formatting.Indented));
                int exitCode = RunSettingsWriter();
                if (exitCode != 0) throw new InvalidOperationException("writer.exe retornou o código " + exitCode + ".");

                settings = JObject.Parse(File.ReadAllText(settingsPath));
                LoadAdvancedSettings();
                SetAdvancedDriverStatus("Device settings applied", true);
                DriverStatus.Text = T("Active");
                DriverStatus.Foreground = new SolidColorBrush(Color.FromRgb(32, 197, 107));
            }
            catch (Exception ex)
            {
                if (backupPath != null && File.Exists(backupPath)) File.Copy(backupPath, settingsPath, true);
                SetAdvancedDriverStatus("Device settings were not applied", false);
                MessageBox.Show(ex.Message, T("Device & Input"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ResetDeviceSettings_Click(object sender, RoutedEventArgs e)
        {
            settings = JObject.Parse(File.ReadAllText(settingsPath));
            LoadAdvancedSettings();
            SetAdvancedDriverStatus("Values reloaded from settings.json", true);
        }

        private void ApplySmoothingSettings_Click(object sender, RoutedEventArgs e)
        {
            string backupPath = null;
            try
            {
                double inputSmoothing = ReadNumber(InputSmoothingBox, "Input Smoothing (ms)");
                double sensitivitySmoothing = ReadNumber(SensitivitySmoothingBox, "Sensitivity Smoothing (ms)");
                double outputSmoothing = ReadNumber(OutputSmoothingBox, "Output Smoothing (ms)");
                double inputSpeedCap = ReadNumber(InputSpeedCapBox, "Input Speed Cap");

                if (inputSmoothing < 0 || inputSmoothing > 100 || sensitivitySmoothing < 0 || sensitivitySmoothing > 100 || outputSmoothing < 0 || outputSmoothing > 100)
                    throw new InvalidDataException(T("Smoothing values must be between 0 and 100 ms."));
                if (inputSpeedCap < 0 || (inputSpeedCap > 0 && inputSpeedCap < 0.10) || inputSpeedCap > 1000)
                    throw new InvalidDataException(T("Input Speed Cap must be 0 or between 0.10 and 1000.00 counts/ms."));

                JObject edited = JObject.Parse(File.ReadAllText(settingsPath));
                JArray profiles = edited["profiles"] as JArray;
                int index = ProfileBox.SelectedIndex < 0 ? 0 : ProfileBox.SelectedIndex;
                if (profiles == null || index < 0 || index >= profiles.Count)
                    throw new InvalidDataException(T("The selected profile was not found."));
                JObject profile = profiles[index] as JObject;
                if (profile == null) throw new InvalidDataException(T("The selected profile was not found."));
                JObject speedCalculation = profile["Input speed calculation parameters"] as JObject;
                if (speedCalculation == null)
                    throw new InvalidDataException(T("Input speed calculation parameters were not found."));

                speedCalculation["Time in ms after which an input is weighted at half its original value."] = inputSmoothing;
                speedCalculation["Time in ms after which scale is weighted at half its original value."] = sensitivitySmoothing;
                speedCalculation["Time in ms after which an output is weighted at half its original value."] = outputSmoothing;
                profile["Input Speed Cap"] = inputSpeedCap;

                Tuple<DriverConfig, string> validation = DriverConfig.Convert(edited.ToString(Formatting.None));
                if (validation == null || validation.Item1 == null)
                    throw new InvalidDataException(T("The original engine rejected the configuration.") + " " + (validation == null ? String.Empty : validation.Item2));

                string backupDirectory = IOPath.Combine(rootDirectory, "backups", "modern-ui");
                Directory.CreateDirectory(backupDirectory);
                backupPath = IOPath.Combine(backupDirectory, "smoothing-settings-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
                File.Copy(settingsPath, backupPath, false);
                File.WriteAllText(settingsPath, edited.ToString(Formatting.Indented));
                int exitCode = RunSettingsWriter();
                if (exitCode != 0) throw new InvalidOperationException("writer.exe retornou o código " + exitCode + ".");

                settings = JObject.Parse(File.ReadAllText(settingsPath));
                LoadAdvancedSettings();
                SetAdvancedDriverStatus("Smoothing settings applied", true);
                DriverStatus.Text = T("Active");
                DriverStatus.Foreground = new SolidColorBrush(Color.FromRgb(32, 197, 107));
            }
            catch (Exception ex)
            {
                if (backupPath != null && File.Exists(backupPath)) File.Copy(backupPath, settingsPath, true);
                SetAdvancedDriverStatus("Smoothing settings were not applied", false);
                MessageBox.Show(ex.Message, T("Smoothing & Stability"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ResetSmoothingSettings_Click(object sender, RoutedEventArgs e)
        {
            settings = JObject.Parse(File.ReadAllText(settingsPath));
            LoadAdvancedSettings();
            SetAdvancedDriverStatus("Values reloaded from settings.json", true);
        }

        private void ApplyAxisSettings_Click(object sender, RoutedEventArgs e)
        {
            string backupPath = null;
            try
            {
                double leftRightRatio = ReadNumber(AxisLeftRightBox, "Left / Right Ratio");
                double upDownRatio = ReadNumber(AxisUpDownBox, "Up / Down Ratio");
                double angleSnapping = ReadNumber(AxisSnapBox, "Angle Snapping (degrees)");

                if (leftRightRatio < 0.10 || leftRightRatio > 10.00 || upDownRatio < 0.10 || upDownRatio > 10.00)
                    throw new InvalidDataException(T("Directional ratios must be between 0.10 and 10.00."));
                if (angleSnapping < 0.00 || angleSnapping > 45.00)
                    throw new InvalidDataException(T("Angle Snapping must be between 0 and 45 degrees."));

                JObject edited = JObject.Parse(File.ReadAllText(settingsPath));
                JArray profiles = edited["profiles"] as JArray;
                int index = ProfileBox.SelectedIndex < 0 ? 0 : ProfileBox.SelectedIndex;
                if (profiles == null || index < 0 || index >= profiles.Count)
                    throw new InvalidDataException(T("The selected profile was not found."));
                JObject profile = profiles[index] as JObject;
                if (profile == null) throw new InvalidDataException(T("The selected profile was not found."));

                profile["L/R output DPI ratio (left sens multiplier)"] = leftRightRatio;
                profile["U/D output DPI ratio (up sens multiplier)"] = upDownRatio;
                profile["Degrees of angle snapping"] = angleSnapping;

                Tuple<DriverConfig, string> validation = DriverConfig.Convert(edited.ToString(Formatting.None));
                if (validation == null || validation.Item1 == null)
                    throw new InvalidDataException(T("The original engine rejected the configuration.") + " " + (validation == null ? String.Empty : validation.Item2));

                string backupDirectory = IOPath.Combine(rootDirectory, "backups", "modern-ui");
                Directory.CreateDirectory(backupDirectory);
                backupPath = IOPath.Combine(backupDirectory, "axis-settings-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
                File.Copy(settingsPath, backupPath, false);
                File.WriteAllText(settingsPath, edited.ToString(Formatting.Indented));
                int exitCode = RunSettingsWriter();
                if (exitCode != 0) throw new InvalidOperationException("writer.exe retornou o código " + exitCode + ".");

                settings = JObject.Parse(File.ReadAllText(settingsPath));
                LoadProfile(index);
                LoadAdvancedSettings();
                SetAdvancedDriverStatus("Axis settings applied", true);
                DriverStatus.Text = T("Active");
                DriverStatus.Foreground = new SolidColorBrush(Color.FromRgb(32, 197, 107));
            }
            catch (Exception ex)
            {
                if (backupPath != null && File.Exists(backupPath)) File.Copy(backupPath, settingsPath, true);
                SetAdvancedDriverStatus("Axis settings were not applied", false);
                MessageBox.Show(ex.Message, T("Axis Tuning"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ResetAxisSettings_Click(object sender, RoutedEventArgs e)
        {
            settings = JObject.Parse(File.ReadAllText(settingsPath));
            int index = ProfileBox.SelectedIndex < 0 ? 0 : ProfileBox.SelectedIndex;
            LoadProfile(index);
            LoadAdvancedSettings();
            SetAdvancedDriverStatus("Values reloaded from settings.json", true);
        }

        private void TestDriver_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DriverConfig active = DriverConfig.GetActive();
                int profiles = active == null || active.profiles == null ? 0 : active.profiles.Count;
                SetAdvancedDriverStatus(profiles > 0 ? "Connected — acceleration active" : "Connected — acceleration disabled", true);
            }
            catch (Exception ex)
            {
                SetAdvancedDriverStatus("Driver communication failed", false);
                MessageBox.Show(ex.Message, T("Driver Control"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyCurrentProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int exitCode = RunSettingsWriter();
                if (exitCode != 0) throw new InvalidOperationException("writer.exe retornou o código " + exitCode + ".");
                SetAdvancedDriverStatus("Connected — current profile applied", true);
                DriverStatus.Text = T("Active");
                DriverStatus.Foreground = new SolidColorBrush(Color.FromRgb(32, 197, 107));
            }
            catch (Exception ex)
            {
                SetAdvancedDriverStatus("Profile apply failed", false);
                MessageBox.Show(ex.Message, T("Driver Control"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisableAcceleration_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DriverConfig.Deactivate();
                SetAdvancedDriverStatus("Connected — acceleration disabled", true);
                DriverStatus.Text = T("Disabled");
                DriverStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 188, 82));
            }
            catch (Exception ex)
            {
                SetAdvancedDriverStatus("Could not disable acceleration", false);
                MessageBox.Show(ex.Message, T("Driver Control"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private JObject CreateEditedSettings()
        {
            JObject edited = (JObject)settings.DeepClone();
            JArray profiles = (JArray)edited["profiles"];
            int index = ProfileBox.SelectedIndex < 0 ? 0 : ProfileBox.SelectedIndex;
            JObject profile = (JObject)profiles[index];
            JObject accel = (JObject)profile["Whole or horizontal accel parameters"];
            JObject domain = (JObject)profile["Stretches domain for horizontal vs vertical inputs"];
            JObject range = (JObject)profile["Stretches accel range for horizontal vs vertical inputs"];

            profile["Output DPI"] = ReadNumber(SensBox, "Sens Multiplier") * 1000.0;
            profile["Y/X output DPI ratio (vertical sens multiplier)"] = ReadNumber(RatioBox, "Y / X Ratio");
            profile["Degrees of rotation"] = ReadNumber(RotationBox, "Rotation");
            accel["mode"] = SelectedMode();
            accel["Gain / Velocity"] = GainToggle.IsChecked == true;
            accel["decayRate"] = ReadNumber(DecayBox, "Decay Rate");
            accel["inputOffset"] = ReadNumber(OffsetBox, "Input Offset");
            accel["limit"] = ReadNumber(LimitBox, "Limit");
            if (domain != null) domain["y"] = ReadNumber(AnisotropyBox, "Anisotropy");
            if (range != null) range["y"] = ReadNumber(VerticalRangeBox, "Vertical Accel Strength");
            return edited;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                JObject edited = CreateEditedSettings();
                Tuple<DriverConfig, string> validation = DriverConfig.Convert(edited.ToString(Formatting.None));
                if (validation == null || validation.Item1 == null)
                    throw new InvalidDataException(T("The configuration was rejected by the original Raw Accel engine.") + " " +
                        (validation == null ? String.Empty : validation.Item2));
                string backupDirectory = IOPath.Combine(rootDirectory, "backups", "modern-ui");
                Directory.CreateDirectory(backupDirectory);
                string backupPath = IOPath.Combine(backupDirectory, "settings-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
                File.Copy(settingsPath, backupPath, false);
                File.WriteAllText(settingsPath, edited.ToString(Formatting.Indented));

                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = IOPath.Combine(rootDirectory, "writer.exe");
                start.Arguments = "\"" + settingsPath + "\"";
                start.WorkingDirectory = rootDirectory;
                start.UseShellExecute = false;
                start.CreateNoWindow = true;
                using (Process process = Process.Start(start))
                {
                    if (process != null) process.WaitForExit(7000);
                }

                string selectedName = ProfileBox.SelectedItem == null ? null : ProfileBox.SelectedItem.ToString();
                LoadSettings(selectedName);
                DriverStatus.Text = T("Active");
                DriverStatus.Foreground = new SolidColorBrush(Color.FromRgb(32, 197, 107));
            }
            catch (Exception ex)
            {
                DriverStatus.Text = T("Apply failed");
                DriverStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 59, 78));
                MessageBox.Show(T("No configuration was applied.") + "\n\n" + ex.Message,
                    "Raw Accel Reimagined", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            string selectedName = ProfileBox.SelectedItem == null ? null : ProfileBox.SelectedItem.ToString();
            LoadSettings(selectedName);
        }

        private void ProfileBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!loading && settings != null && ProfileBox.SelectedIndex >= 0)
            {
                LoadProfile(ProfileBox.SelectedIndex);
                LoadAdvancedSettings();
            }
        }

        private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawChart();
        }

        private void DrawChart()
        {
            if (!IsLoaded || ChartCanvas.ActualWidth < 200 || ChartCanvas.ActualHeight < 150 || settings == null) return;
            ChartCanvas.Children.Clear();
            List<double> values = CalculateCurve();
            if (values.Count == 0) return;
            displayedCurve = values;
            mouseMarker = null;
            mouseGuide = null;
            mouseMarkerLabel = null;

            double width = ChartCanvas.ActualWidth;
            double height = ChartCanvas.ActualHeight;
            double left = 72, right = 24, top = 30, bottom = 58;
            double plotWidth = Math.Max(1, width - left - right);
            double plotHeight = Math.Max(1, height - top - bottom);
            double minValue = values.Min();
            double maxValue = values.Max();
            double yMin = Math.Max(0, Math.Floor(minValue * 100.0) / 100.0);
            double yMax = Math.Max(yMin + 1, Math.Ceiling(maxValue * 1.08 * 100.0) / 100.0);
            chartPlotLeft = left;
            chartPlotTop = top;
            chartPlotWidth = plotWidth;
            chartPlotHeight = plotHeight;
            chartYMin = yMin;
            chartYMax = yMax;

            for (int i = 0; i <= 8; i++)
            {
                double x = left + plotWidth * i / 8.0;
                AddLine(x, top, x, top + plotHeight, i % 2 == 0 ? "#253752" : "#1B2B42", i % 2 == 0 ? 1 : 0.7, i % 2 == 0 ? null : new DoubleCollection(new double[] { 2, 4 }));
                if (i % 2 == 0) AddLabel((i * 5).ToString(), x - 10, top + plotHeight + 9, 42, TextAlignment.Center, "#A8B4C7");
            }
            for (int i = 0; i <= 5; i++)
            {
                double y = top + plotHeight * i / 5.0;
                AddLine(left, y, left + plotWidth, y, "#263852", 1, new DoubleCollection(new double[] { 2, 4 }));
                double labelValue = yMax - (yMax - yMin) * i / 5.0;
                AddLabel(labelValue.ToString("0.##", CultureInfo.InvariantCulture), 4, y - 10, 58, TextAlignment.Right, "#A8B4C7");
            }

            PointCollection curve = new PointCollection();
            PointCollection area = new PointCollection();
            area.Add(new Point(left, top + plotHeight));
            for (int i = 0; i < values.Count; i++)
            {
                double x = left + plotWidth * i / (values.Count - 1.0);
                double y = top + plotHeight - (values[i] - yMin) / (yMax - yMin) * plotHeight;
                Point point = new Point(x, y);
                curve.Add(point);
                area.Add(point);
            }
            area.Add(new Point(left + plotWidth, top + plotHeight));
            Polygon fill = new Polygon();
            fill.Points = area;
            fill.Fill = new LinearGradientBrush(Color.FromArgb(90, 21, 148, 251), Color.FromArgb(5, 21, 148, 251), new Point(0, 0), new Point(0, 1));
            ChartCanvas.Children.Add(fill);
            Polyline line = new Polyline();
            line.Points = curve;
            line.Stroke = new SolidColorBrush(Color.FromRgb(21, 148, 251));
            line.StrokeThickness = 3;
            ChartCanvas.Children.Add(line);
            PositionMouseMarker();

            AddLabel(T("Input Speed (counts/ms)"), left + plotWidth / 2 - 100, height - 32, 200, TextAlignment.Center, "#DCE6F4");
            TextBlock vertical = new TextBlock();
            vertical.Text = T("Ratio of Output to Input");
            vertical.Foreground = new SolidColorBrush(Color.FromRgb(220, 230, 244));
            vertical.RenderTransform = new RotateTransform(-90);
            Canvas.SetLeft(vertical, 14);
            Canvas.SetTop(vertical, top + plotHeight / 2 + 80);
            ChartCanvas.Children.Add(vertical);

            int atTen = Math.Min(values.Count - 1, (int)Math.Round((values.Count - 1) * 10.0 / 40.0));
            StatOutput.Text = values[atTen].ToString("0.00", CultureInfo.InvariantCulture);
            StatX.Text = (10 * values[atTen]).ToString("0.0", CultureInfo.InvariantCulture);
            StatY.Text = (10 * values[atTen] * ParseOrDefault(RatioBox.Text, 1)).ToString("0.0", CultureInfo.InvariantCulture);
        }

        private void PositionMouseMarker()
        {
            if (!hasMouseMarker || displayedCurve.Count < 2 || chartPlotWidth <= 0 || chartYMax <= chartYMin) return;
            double shownInput = Math.Max(0, Math.Min(40, lastMarkerInputSpeed));
            double shownRatio = CurveRatioAt(shownInput);
            double x = chartPlotLeft + shownInput / 40.0 * chartPlotWidth;
            double y = chartPlotTop + chartPlotHeight - (shownRatio - chartYMin) / (chartYMax - chartYMin) * chartPlotHeight;
            y = Math.Max(chartPlotTop, Math.Min(chartPlotTop + chartPlotHeight, y));

            if (mouseGuide == null)
            {
                mouseGuide = new Line();
                mouseGuide.Stroke = new SolidColorBrush(Color.FromArgb(150, 255, 59, 78));
                mouseGuide.StrokeThickness = 1;
                mouseGuide.StrokeDashArray = new DoubleCollection(new double[] { 3, 4 });
                Panel.SetZIndex(mouseGuide, 20);
                ChartCanvas.Children.Add(mouseGuide);
            }
            mouseGuide.X1 = x;
            mouseGuide.X2 = x;
            mouseGuide.Y1 = y;
            mouseGuide.Y2 = chartPlotTop + chartPlotHeight;

            if (mouseMarker == null)
            {
                mouseMarker = new Ellipse();
                mouseMarker.Width = 13;
                mouseMarker.Height = 13;
                mouseMarker.Fill = new SolidColorBrush(Color.FromRgb(255, 59, 78));
                mouseMarker.Stroke = new SolidColorBrush(Color.FromRgb(255, 190, 198));
                mouseMarker.StrokeThickness = 2;
                Panel.SetZIndex(mouseMarker, 22);
                ChartCanvas.Children.Add(mouseMarker);
            }
            Canvas.SetLeft(mouseMarker, x - mouseMarker.Width / 2);
            Canvas.SetTop(mouseMarker, y - mouseMarker.Height / 2);

            if (mouseMarkerLabel == null)
            {
                TextBlock labelText = new TextBlock();
                labelText.Foreground = Brushes.White;
                labelText.FontSize = 12;
                labelText.Padding = new Thickness(7, 3, 7, 3);
                mouseMarkerLabel = new Border();
                mouseMarkerLabel.Background = new SolidColorBrush(Color.FromArgb(235, 33, 17, 29));
                mouseMarkerLabel.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 59, 78));
                mouseMarkerLabel.BorderThickness = new Thickness(1);
                mouseMarkerLabel.CornerRadius = new CornerRadius(5);
                mouseMarkerLabel.Child = labelText;
                Panel.SetZIndex(mouseMarkerLabel, 23);
                ChartCanvas.Children.Add(mouseMarkerLabel);
            }
            TextBlock markerText = (TextBlock)mouseMarkerLabel.Child;
            markerText.Text = shownInput.ToString("0.0", CultureInfo.InvariantCulture) + " counts/ms  •  " + shownRatio.ToString("0.00", CultureInfo.InvariantCulture) + "×";
            mouseMarkerLabel.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            double labelWidth = mouseMarkerLabel.DesiredSize.Width;
            double labelLeft = x + 11;
            if (labelLeft + labelWidth > chartPlotLeft + chartPlotWidth) labelLeft = x - labelWidth - 11;
            Canvas.SetLeft(mouseMarkerLabel, Math.Max(chartPlotLeft, labelLeft));
            Canvas.SetTop(mouseMarkerLabel, Math.Max(chartPlotTop, y - 38));
        }

        private List<double> CalculateCurve()
        {
            List<double> values = new List<double>();
            try
            {
                JObject edited = CreateEditedSettings();
                Tuple<DriverConfig, string> conversion = DriverConfig.Convert(edited.ToString(Formatting.None));
                if (conversion == null || conversion.Item1 == null) throw new InvalidDataException(conversion == null ? "Driver conversion failed" : conversion.Item2);
                int index = ProfileBox.SelectedIndex < 0 ? 0 : ProfileBox.SelectedIndex;
                Profile profile = conversion.Item1.profiles[index];
                using (ManagedAccel baseAccel = new ManagedAccel(profile))
                {
                    for (int i = 0; i <= 100; i++)
                    {
                        double speed = Math.Max(0.01, 40.0 * i / 100.0);
                        using (ManagedAccel sample = baseAccel.CreateStatelessCopy())
                        {
                            Tuple<double, double> output = sample.Accelerate(1000, 0, 1.0, 1000.0 / speed);
                            values.Add(Math.Max(0, output.Item1 / 1000.0));
                        }
                    }
                }
            }
            catch
            {
                double sens = ParseOrDefault(SensBox.Text, 1);
                double offset = ParseOrDefault(OffsetBox.Text, 0);
                double decay = Math.Max(0.01, ParseOrDefault(DecayBox.Text, 1));
                double limit = Math.Max(0, ParseOrDefault(LimitBox.Text, 1));
                for (int i = 0; i <= 100; i++)
                {
                    double speed = 40.0 * i / 100.0;
                    double rise = speed <= offset ? 0 : (1.0 - Math.Exp(-(speed - offset) * decay / 22.0));
                    values.Add(sens * (1.0 + rise * limit / 2.0));
                }
            }
            return values;
        }

        private static double ParseOrDefault(string text, double fallback)
        {
            double value;
            return Double.TryParse((text ?? "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : fallback;
        }

        private void AddLine(double x1, double y1, double x2, double y2, string color, double thickness, DoubleCollection dash)
        {
            Line line = new Line();
            line.X1 = x1; line.Y1 = y1; line.X2 = x2; line.Y2 = y2;
            line.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom(color);
            line.StrokeThickness = thickness;
            line.StrokeDashArray = dash;
            ChartCanvas.Children.Add(line);
        }

        private void AddLabel(string text, double left, double top, double width, TextAlignment alignment, string color)
        {
            TextBlock label = new TextBlock();
            label.Text = text;
            label.Width = width;
            label.TextAlignment = alignment;
            label.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(color);
            Canvas.SetLeft(label, left);
            Canvas.SetTop(label, top);
            ChartCanvas.Children.Add(label);
        }

        private void OpenClassic_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(IOPath.Combine(rootDirectory, "rawaccel.exe")) { WorkingDirectory = rootDirectory });
        }

        private void Themes_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(T("The modern interface uses the reference palette. Classic themes remain available in the original rawaccel.exe."), T("Themes"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(IOPath.Combine(rootDirectory, "doc", "Guide.md")) { UseShellExecute = true });
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) Maximize_Click(sender, e);
            else if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) { HideToTray(); }
        private void Maximize_Click(object sender, RoutedEventArgs e) { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }
        private void Close_Click(object sender, RoutedEventArgs e) { HideToTray(); }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;       // ← faltaba este, necesario para .All()
using System.Threading;

// ════════════════════════════════════════════════════════════
//  CLASE: Cajero  (Login)
//  Punto de entrada del programa. Autentica al usuario
//  leyendo número de cuenta y NIP desde el archivo .txt
// ════════════════════════════════════════════════════════════
class Cajero
{
    const int NIP_LENGTH = 4;
    const int MAX_INTENTOS = 3;
    const string TITULO = "╔══════════════════════════════╗\n" +
                                "║      CAJERO EASYBANK ATM     ║\n" +
                                "╚══════════════════════════════╝";

    static readonly ConsoleColor COLOR_TITULO = ConsoleColor.Blue;
    static readonly ConsoleColor COLOR_ERROR = ConsoleColor.Red;
    static readonly ConsoleColor COLOR_OK = ConsoleColor.Green;
    static readonly ConsoleColor COLOR_INFO = ConsoleColor.Yellow;
    static readonly ConsoleColor COLOR_NORMAL = ConsoleColor.White;

    static int _filaBase;

    // ─── Punto de entrada del programa ─────────────────────
    static void Main()
    {
        Console.CursorVisible = false;
        Console.Clear();

        // Carga todas las cuentas del archivo al iniciar
        // Si el archivo no existe, LeerTodasLasCuentas() lo crea automaticamente
        var todasLasCuentas = ATM.LeerTodasLasCuentas();

        DibujarTitulo();

        int intentos = 0;
        bool autenticado = false;
        string cuentaActiva = null;

        while (intentos < MAX_INTENTOS && !autenticado)
        {
            LimpiarLineasMensaje();

            // ── Paso 1: pide número de cuenta ────────────────
            string numeroCuenta = LeerCampo("Numero de cuenta: ", visible: true);

            if (!todasLasCuentas.ContainsKey(numeroCuenta))
            {
                intentos++;
                MostrarError("Cuenta no encontrada.", intentos);
                continue;
            }

            // ── Paso 2: pide NIP ─────────────────────────────
            LimpiarLineasMensaje();
            string nipIngresado = LeerCampo("NIP (4 digitos): ", visible: false);
            string errorNip = ValidarNip(nipIngresado);

            if (errorNip != null)
            {
                intentos++;
                MostrarError(errorNip, intentos);
                continue;
            }

            // Verifica que el NIP coincida con el guardado en el archivo
            string nipGuardado = todasLasCuentas[numeroCuenta]["nip"];
            if (nipIngresado != nipGuardado)
            {
                intentos++;
                MostrarError("NIP incorrecto.", intentos);
                continue;
            }

            // ── Login exitoso ────────────────────────────────
            autenticado = true;
            cuentaActiva = numeroCuenta;
            string titular = todasLasCuentas[numeroCuenta]["titular"];
            MostrarMensaje($"Bienvenido, {titular}!", COLOR_OK);
            Thread.Sleep(1500);
        }

        // ── Bloqueo por intentos agotados ────────────────────
        if (!autenticado)
        {
            MostrarMensaje("Tarjeta bloqueada. Contacte a su banco.", COLOR_ERROR);
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.ResetColor();
            Console.ReadKey();
            return;
        }

        // ── Lanza el cajero con la cuenta autenticada ────────
        ATM.Iniciar(cuentaActiva);
    }

    // ─── Dibuja el título centrado en la consola ────────────
    static void DibujarTitulo()
    {
        string[] lineas = TITULO.Split('\n');
        int anchoMax = 0;
        foreach (var l in lineas) anchoMax = Math.Max(anchoMax, l.Length);

        int fila = Console.WindowHeight / 2 - lineas.Length - 3;

        Console.ForegroundColor = COLOR_TITULO;
        foreach (var linea in lineas)
            Centrar(linea, fila++);

        Console.ResetColor();
        _filaBase = fila + 1;
    }

    // ─── Limpia las líneas del área de mensajes ─────────────
    static void LimpiarLineasMensaje()
    {
        for (int i = _filaBase; i <= _filaBase + 3; i++)
        {
            Console.SetCursorPosition(0, i);
            Console.Write(new string(' ', Console.WindowWidth));
        }
    }

    // ─── Lee un campo de texto, con o sin máscara ───────────
    // visible: true  → muestra el texto normal (número de cuenta)
    // visible: false → muestra "*" por cada carácter (NIP)
    static string LeerCampo(string prompt, bool visible)
    {
        int x = Math.Max(0, (Console.WindowWidth - prompt.Length - NIP_LENGTH) / 2);
        Console.ForegroundColor = COLOR_INFO;
        Console.SetCursorPosition(x, _filaBase);
        Console.Write(prompt);
        Console.ForegroundColor = COLOR_NORMAL;
        Console.CursorVisible = true;

        string resultado = visible ? (Console.ReadLine() ?? "").Trim() : LeerConMascara();

        Console.CursorVisible = false;
        return resultado;
    }

    // ─── Lee texto ocultando cada carácter con "*" ──────────
    static string LeerConMascara()
    {
        var sb = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;

            if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar) && sb.Length < NIP_LENGTH)
            {
                sb.Append(key.KeyChar);
                Console.Write('*');
            }
        }
        Console.WriteLine();
        return sb.ToString();
    }

    // ─── Muestra error con intentos restantes ───────────────
    static void MostrarError(string mensaje, int intentos)
    {
        int restantes = MAX_INTENTOS - intentos;
        string texto = restantes > 0
            ? $"X  {mensaje}  ({restantes} intento{(restantes > 1 ? "s" : "")} restante)"
            : $"X  {mensaje}";

        MostrarMensaje(texto, COLOR_ERROR);
        if (restantes > 0) Thread.Sleep(1600);
    }

    // ─── Muestra un mensaje centrado en el área de mensajes ─
    static void MostrarMensaje(string msg, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Centrar(msg, _filaBase + 2);
        Console.ResetColor();
    }

    // ─── Valida formato y longitud del NIP ──────────────────
    static string ValidarNip(string nip)
    {
        if (nip.Length == 0) return "El NIP no puede estar vacio.";
        if (!nip.All(c => char.IsDigit(c))) return "El NIP solo puede contener numeros.";
        if (nip.Length < NIP_LENGTH) return $"El NIP debe tener {NIP_LENGTH} digitos.";
        if (nip.Length > NIP_LENGTH) return $"El NIP no puede tener mas de {NIP_LENGTH} digitos.";
        return null;
    }

    // ─── Centra texto en una fila específica ────────────────
    static void Centrar(string texto, int fila)
    {
        int x = Math.Max(0, (Console.WindowWidth - texto.Length) / 2);
        Console.SetCursorPosition(x, fila);
        Console.Write(texto);
    }
}


// ════════════════════════════════════════════════════════════
//  CLASE: ATM  (operaciones del cajero)
//  Recibe el número de cuenta desde Cajero (Login) y opera
//  únicamente sobre ese bloque en el archivo cuenta.txt
// ════════════════════════════════════════════════════════════
class ATM
{
    const string DB_PATH = "cuenta.txt";
    const int LIMITE_DEPOSITO = 5000;

    // ─── Helpers de consola ─────────────────────────────────
    static void PrintC(string text, ConsoleColor color = ConsoleColor.DarkBlue)
    {
        Console.ForegroundColor = color;
        int padding = Math.Max(0, (Console.WindowWidth - text.Length) / 2);
        Console.WriteLine(new string(' ', padding) + text);
    }

    static void WriteC(string text, ConsoleColor color = ConsoleColor.Red)
    {
        Console.ForegroundColor = color;
        int padding = Math.Max(0, (Console.WindowWidth - text.Length) / 2);
        Console.Write(new string(' ', padding) + text);
    }

    static int LeerEntero()
    {
        int valor;
        while (!int.TryParse(Console.ReadLine(), out valor))
            WriteC("Entrada invalida. Intente de nuevo: ");
        return valor;
    }

    // ─── Lee TODAS las cuentas del archivo ──────────────────
    // Si el archivo no existe lo crea con 3 cuentas de ejemplo
    // Es public porque Cajero.Main() lo llama para el login
    public static Dictionary<string, Dictionary<string, string>> LeerTodasLasCuentas()
    {
        var cuentas = new Dictionary<string, Dictionary<string, string>>();

        // Crea el archivo con cuentas de ejemplo si no existe
        if (!File.Exists(DB_PATH))
        {
            File.WriteAllLines(DB_PATH, new[]
            {
                "[000-123-456]",
                "titular=Juan Perez",
                "nip=1234",
                "saldo=1000",
                "",
                "[000-789-012]",
                "titular=Maria Garcia",
                "nip=5678",
                "saldo=2500",
                "",
                "[000-345-678]",
                "titular=Carlos Lopez",
                "nip=9012",
                "saldo=500"
            });
        }

        string cuentaActual = null;

        foreach (var linea in File.ReadAllLines(DB_PATH))
        {
            var l = linea.Trim();

            // Detecta encabezado [numero-cuenta] e inicia nuevo bloque
            if (l.StartsWith("[") && l.EndsWith("]"))
            {
                cuentaActual = l.Trim('[', ']');
                cuentas[cuentaActual] = new Dictionary<string, string>();
                continue;
            }

            // Agrega clave=valor al bloque activo
            if (cuentaActual != null && l.Contains("="))
            {
                var partes = l.Split('=');
                if (partes.Length == 2)
                    cuentas[cuentaActual][partes[0].Trim()] = partes[1].Trim();
            }
        }

        return cuentas;
    }

    // ─── Lee solo el bloque de la cuenta indicada ───────────
    static Dictionary<string, string> LeerCuenta(string numeroCuenta)
    {
        var datos = new Dictionary<string, string>();
        bool leyendoCuenta = false;

        foreach (var linea in File.ReadAllLines(DB_PATH))
        {
            var l = linea.Trim();

            if (l == $"[{numeroCuenta}]") { leyendoCuenta = true; continue; }
            if (leyendoCuenta && l.StartsWith("[")) break;

            if (leyendoCuenta && l.Contains("="))
            {
                var partes = l.Split('=');
                if (partes.Length == 2)
                    datos[partes[0].Trim()] = partes[1].Trim();
            }
        }

        return datos;
    }

    // ─── Guarda solo el bloque de la cuenta activa ──────────
    // Reescribe el archivo completo pero modifica
    // únicamente el bloque correspondiente
    static void GuardarCuenta(string numeroCuenta, Dictionary<string, string> datosNuevos)
    {
        var lineas = File.ReadAllLines(DB_PATH);
        var resultado = new List<string>();
        bool enBloque = false;

        foreach (var linea in lineas)
        {
            var l = linea.Trim();

            if (l == $"[{numeroCuenta}]")
            {
                enBloque = true;
                resultado.Add(linea);
                foreach (var par in datosNuevos)
                    resultado.Add($"{par.Key}={par.Value}");
                continue;
            }

            if (enBloque && (l.StartsWith("[") || l == ""))
                enBloque = false;

            if (!enBloque)
                resultado.Add(linea);
        }

        File.WriteAllLines(DB_PATH, resultado);
    }

    // ─── Operación: Retiro ───────────────────────────────────
    static void Retirar(int monto, string numeroCuenta, Dictionary<string, string> db)
    {
        decimal saldo = decimal.Parse(db["saldo"]);

        if (monto <= 0) { PrintC("La cantidad debe ser mayor a 0.", ConsoleColor.Red); return; }
        if (monto > saldo) { PrintC("Saldo insuficiente.", ConsoleColor.Red); return; }

        saldo -= monto;
        db["saldo"] = saldo.ToString();
        GuardarCuenta(numeroCuenta, db);

        PrintC($"Retiro de ${monto} exitoso.", ConsoleColor.Green);
        PrintC($"   Saldo actual: {saldo:C}", ConsoleColor.Yellow);
    }

    // ─── Operación: Depósito ────────────────────────────────
    static void Depositar(int monto, string numeroCuenta, Dictionary<string, string> db)
    {
        decimal saldo = decimal.Parse(db["saldo"]);

        if (monto <= 0)
        {
            PrintC("La cantidad debe ser mayor a 0.", ConsoleColor.Red);
            return;
        }
        if (monto > LIMITE_DEPOSITO)
        {
            PrintC($"No puede depositar mas de ${LIMITE_DEPOSITO} por operacion.", ConsoleColor.Red);
            return;
        }
        if (saldo + monto > LIMITE_DEPOSITO)
        {
            decimal disponible = LIMITE_DEPOSITO - saldo;
            PrintC($"Su saldo superaria el limite de ${LIMITE_DEPOSITO}.", ConsoleColor.Red);
            PrintC($"   Puede depositar maximo: ${disponible}", ConsoleColor.Yellow);
            return;
        }

        saldo += monto;
        db["saldo"] = saldo.ToString();
        GuardarCuenta(numeroCuenta, db);

        PrintC($"Deposito de ${monto} exitoso.", ConsoleColor.Green);
        PrintC($"   Saldo actual: {saldo:C}", ConsoleColor.Yellow);
    }

    // ─── Operación: Pago de servicio ────────────────────────
    static void PagarServicio(string servicio, int monto, string numeroCuenta, Dictionary<string, string> db)
    {
        decimal saldo = decimal.Parse(db["saldo"]);

        if (monto > saldo)
        {
            PrintC($"Saldo insuficiente para pagar {servicio}.", ConsoleColor.Red);
            return;
        }

        saldo -= monto;
        db["saldo"] = saldo.ToString();
        GuardarCuenta(numeroCuenta, db);

        PrintC($"Pago de {servicio} por ${monto} realizado.", ConsoleColor.Green);
        PrintC($"   Saldo actual: {saldo:C}", ConsoleColor.Yellow);
    }

    // ─── Menú principal del cajero ──────────────────────────
    // Llamado por Cajero.Main() una vez autenticado el usuario
    public static void Iniciar(string numeroCuenta)
    {
        while (true)
        {
            Console.Clear();

            var db = LeerCuenta(numeroCuenta);
            decimal saldo = decimal.Parse(db["saldo"]);
            string titular = db["titular"];

            PrintC("╔════════════════════════════════════════════╗");
            PrintC("║        BIENVENIDO AL CAJERO EASYBANK       ║");
            PrintC("╚════════════════════════════════════════════╝");
            PrintC($"Titular : {titular}", ConsoleColor.Cyan);
            PrintC($"Cuenta  : {numeroCuenta}", ConsoleColor.Cyan);
            PrintC($"Saldo   : {saldo:C}", ConsoleColor.Yellow);
            PrintC("────────────────────────────────────────────");
            PrintC("(1) Retiro");
            PrintC("(2) Pago de servicios");
            PrintC("(3) Depositos");
            PrintC("(0) Salir");
            PrintC("────────────────────────────────────────────");
            WriteC("» ");

            int op = LeerEntero();
            Console.WriteLine();

            switch (op)
            {
                case 1:
                    PrintC("•••••  RETIROS  •••••");
                    PrintC("[1] $50      [2] $100     [3] $150", ConsoleColor.Green);
                    PrintC("[4] $200     [5] $250     [6] $500", ConsoleColor.Green);
                    PrintC("[7] Otra cantidad", ConsoleColor.Green);
                    PrintC("[0] Volver al menu", ConsoleColor.DarkGray);
                    PrintC("────────────────────────────────────────────");
                    WriteC("» ");

                    var montos = new Dictionary<int, int>
                        { {1,50},{2,100},{3,150},{4,200},{5,250},{6,500} };

                    int opc = LeerEntero();
                    if (opc == 0) break;

                    if (montos.TryGetValue(opc, out int monto))
                        Retirar(monto, numeroCuenta, db);
                    else if (opc == 7)
                    {
                        PrintC("Ingrese la cantidad a retirar:");
                        WriteC("$ ");
                        Retirar(LeerEntero(), numeroCuenta, db);
                    }
                    else PrintC("Opcion no valida.", ConsoleColor.Red);
                    break;

                case 2:
                    PrintC("•••••  PAGO DE SERVICIOS  •••••");
                    PrintC("────────────────────────────────────────────");
                    PrintC("[1] Internet   - $350", ConsoleColor.Green);
                    PrintC("[2] Luz        - $500", ConsoleColor.Green);
                    PrintC("[3] Agua       - $200", ConsoleColor.Green);
                    PrintC("[4] Gas        - $300", ConsoleColor.Green);
                    PrintC("[5] Telefono   - $250", ConsoleColor.Green);
                    PrintC("[6] Television - $400", ConsoleColor.Green);
                    PrintC("[0] Volver al menu", ConsoleColor.DarkGray);
                    PrintC("────────────────────────────────────────────");
                    WriteC("» ");

                    var servicios = new Dictionary<int, (string nombre, int costo)>
                    {
                        {1,("Internet",350)},{2,("Luz",500)},{3,("Agua",200)},
                        {4,("Gas",300)},{5,("Telefono",250)},{6,("Television",400)}
                    };

                    int opcServicio = LeerEntero();
                    if (opcServicio == 0) break;

                    if (servicios.TryGetValue(opcServicio, out var servicio))
                    {
                        PrintC("────────────────────────────────────────────");
                        PrintC($"  Servicio : {servicio.nombre}", ConsoleColor.Cyan);
                        PrintC($"  Monto    : ${servicio.costo}", ConsoleColor.Cyan);
                        PrintC("────────────────────────────────────────────");
                        PrintC("Confirmar pago? (1) Si   (0) No", ConsoleColor.Yellow);
                        WriteC("» ");

                        if (LeerEntero() == 1)
                            PagarServicio(servicio.nombre, servicio.costo, numeroCuenta, db);
                        else
                            PrintC("Pago cancelado.", ConsoleColor.Red);
                    }
                    else PrintC("Opcion no valida.", ConsoleColor.Red);
                    break;

                case 3:
                    PrintC("•••••  DEPOSITOS  •••••");
                    PrintC($"  (Limite por operacion: ${LIMITE_DEPOSITO})", ConsoleColor.DarkGray);
                    PrintC("[1] $50      [2] $100     [3] $150", ConsoleColor.Green);
                    PrintC("[4] $200     [5] $250     [6] $500", ConsoleColor.Green);
                    PrintC("[7] Otra cantidad", ConsoleColor.Green);
                    PrintC("[0] Volver al menu", ConsoleColor.DarkGray);
                    PrintC("────────────────────────────────────────────");
                    WriteC("» ");

                    var montosDeposito = new Dictionary<int, int>
                        { {1,50},{2,100},{3,150},{4,200},{5,250},{6,500} };

                    int opcDeposito = LeerEntero();
                    if (opcDeposito == 0) break;

                    if (montosDeposito.TryGetValue(opcDeposito, out int montoDeposito))
                        Depositar(montoDeposito, numeroCuenta, db);
                    else if (opcDeposito == 7)
                    {
                        PrintC("Ingrese la cantidad a depositar:");
                        WriteC("$ ");
                        Depositar(LeerEntero(), numeroCuenta, db);
                    }
                    else PrintC("Opcion no valida.", ConsoleColor.Red);
                    break;

                case 0:
                    Console.Clear();
                    PrintC("╔════════════════════════════════════════════╗");
                    PrintC("║         GRACIAS POR USAR EL CAJERO         ║");
                    PrintC("╚════════════════════════════════════════════╝");
                    Console.ResetColor();
                    return;

                default:
                    PrintC("Opcion no valida.", ConsoleColor.Red);
                    break;
            }

            Console.WriteLine();
            PrintC("Presione cualquier tecla para volver al menu...", ConsoleColor.DarkGray);
            Console.ReadKey();
        }
    }
}

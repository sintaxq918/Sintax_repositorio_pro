using System;
using System.Collections.Generic;
using System.IO;

class ATM
{
    // ─── Constantes globales ────────────────────────────────
    // Se definen aquí arriba para modificarlas fácilmente
    const string DB_PATH = "cuenta.txt";
    const int LIMITE_DEPOSITO = 5000;  // ← movida aquí desde la mitad del código

    // ─── Imprime texto centrado en la consola ──────────────
    static void PrintC(string text, ConsoleColor color = ConsoleColor.DarkBlue)
    {
        Console.ForegroundColor = color;
        int padding = Math.Max(0, (Console.WindowWidth - text.Length) / 2);
        Console.WriteLine(new string(' ', padding) + text);
    }

    // ─── Igual que PrintC pero sin salto de línea al final ─
    // Se usa para el cursor de entrada "» " o "$ "
    static void WriteC(string text, ConsoleColor color = ConsoleColor.Red)
    {
        Console.ForegroundColor = color;
        int padding = Math.Max(0, (Console.WindowWidth - text.Length) / 2);
        Console.Write(new string(' ', padding) + text);
    }

    // ─── Lee un número entero desde la consola ─────────────
    // Si el usuario escribe algo que no es número,
    // muestra un error y vuelve a pedir el dato
    static int LeerEntero()
    {
        int valor;
        while (!int.TryParse(Console.ReadLine(), out valor))
            WriteC("Entrada inválida. Intente de nuevo: ");
        return valor;
    }

    // ─── Lee el archivo .txt y lo convierte a un Dictionary ─
    // Cada línea tiene el formato "clave=valor"
    // Si el archivo no existe, lo crea con valores por defecto
    static Dictionary<string, string> LeerDB()
    {
        var datos = new Dictionary<string, string>();

        if (!File.Exists(DB_PATH))
        {
            // Crea el archivo con datos iniciales si no existe
            File.WriteAllLines(DB_PATH, new[]
            {
                "titular=Usuario",
                "saldo=1000",
                "cuenta=000-123-456"
            });
        }

        // Lee cada línea y la separa en clave y valor por el "="
        foreach (var linea in File.ReadAllLines(DB_PATH))
        {
            var partes = linea.Split('=');
            if (partes.Length == 2)
                datos[partes[0].Trim()] = partes[1].Trim();
        }

        return datos;
    }

    // ─── Guarda el Dictionary de vuelta al archivo .txt ────
    // Sobreescribe el archivo completo con los datos actualizados
    static void GuardarDB(Dictionary<string, string> datos)
    {
        var lineas = new List<string>();
        foreach (var par in datos)
            lineas.Add($"{par.Key}={par.Value}");
        File.WriteAllLines(DB_PATH, lineas);
    }

    // ─── Realiza el retiro y actualiza el saldo ─────────────
    // Valida que el monto sea positivo y que haya saldo suficiente
    static void Retirar(int monto, Dictionary<string, string> db)
    {
        decimal saldo = decimal.Parse(db["saldo"]);

        // Validación: el monto debe ser mayor a 0
        if (monto <= 0)
        {
            PrintC(" La cantidad debe ser mayor a 0.", ConsoleColor.Red);
            return;
        }

        // Validación: no se puede retirar más de lo que hay
        if (monto > saldo)
        {
            PrintC(" Saldo insuficiente.", ConsoleColor.Red);
            return;
        }

        // Descuenta el monto, actualiza el Dictionary y guarda en el .txt
        saldo -= monto;
        db["saldo"] = saldo.ToString();
        GuardarDB(db);

        PrintC($" Retiro de ${monto} exitoso.", ConsoleColor.Green);
        PrintC($"   Saldo actual: {saldo:C}", ConsoleColor.Yellow);
    }

    // ─── Realiza el depósito y actualiza el saldo ───────────
    // ⚠ CORREGIDO: se eliminó el método duplicado que existía sin validaciones
    // Esta es la versión correcta y única, con todas las validaciones
    static void Depositar(int monto, Dictionary<string, string> db)
    {
        decimal saldo = decimal.Parse(db["saldo"]);

        // Validación: el monto debe ser mayor a 0
        if (monto <= 0)
        {
            PrintC(" La cantidad debe ser mayor a 0.", ConsoleColor.Red);
            return;
        }

        // Validación: el monto no puede superar el límite por operación
        if (monto > LIMITE_DEPOSITO)
        {
            PrintC($" No puede depositar más de ${LIMITE_DEPOSITO} por operación.", ConsoleColor.Red);
            return;
        }

        // Validación: el saldo total no puede superar el límite
        if (saldo + monto > LIMITE_DEPOSITO)
        {
            decimal disponible = LIMITE_DEPOSITO - saldo;
            PrintC($" Su saldo superaría el límite de ${LIMITE_DEPOSITO}.", ConsoleColor.Red);
            PrintC($"   Puede depositar máximo: ${disponible}", ConsoleColor.Yellow);
            return;
        }

        // Suma el monto, actualiza el Dictionary y guarda en el .txt
        saldo += monto;
        db["saldo"] = saldo.ToString();
        GuardarDB(db);

        PrintC($"Depósito de ${monto} exitoso.", ConsoleColor.Green);
        PrintC($"   Saldo actual: {saldo:C}", ConsoleColor.Yellow);
    }

    // ─── Realiza el pago de un servicio y actualiza el saldo ─
    // Valida que haya saldo suficiente, descuenta el monto
    static void PagarServicio(string servicio, int monto, Dictionary<string, string> db)
    {
        decimal saldo = decimal.Parse(db["saldo"]);

        // Validación: no se puede pagar si no hay saldo suficiente
        if (monto > saldo)
        {
            PrintC($"❌ Saldo insuficiente para pagar {servicio}.", ConsoleColor.Red);
            return;
        }

        // Descuenta el monto, actualiza el Dictionary y guarda en el .txt
        saldo -= monto;
        db["saldo"] = saldo.ToString();
        GuardarDB(db);

        PrintC($"✅ Pago de {servicio} por ${monto} realizado.", ConsoleColor.Green);
        PrintC($"   Saldo actual: {saldo:C}", ConsoleColor.Yellow);
    }

    // ─── Punto de entrada del programa ─────────────────────
    static void Main()
    {
        // Bucle principal: mantiene el menú activo hasta que
        // el usuario elija la opción (0) Salir
        while (true)
        {
            Console.Clear();

            // Carga los datos frescos del .txt en cada vuelta
            // para reflejar siempre el saldo actualizado
            var db = LeerDB();
            decimal saldo = decimal.Parse(db["saldo"]);
            string titular = db["titular"];
            string cuenta = db["cuenta"];

            // ── Muestra el menú principal centrado ──────────
            PrintC("╔════════════════════════════════════════════╗");
            PrintC("║        BIENVENIDO AL CAJERO EASYBANK       ║");
            PrintC("╚════════════════════════════════════════════╝");
            PrintC($"Titular : {titular}", ConsoleColor.Cyan);
            PrintC($"Cuenta  : {cuenta}", ConsoleColor.Cyan);
            PrintC($"Saldo   : {saldo:C}", ConsoleColor.Yellow);
            PrintC("────────────────────────────────────────────");
            PrintC("(1) Retiro");
            PrintC("(2) Pago de servicios");
            PrintC("(3) Depósitos");
            PrintC("(0) Salir");
            PrintC("────────────────────────────────────────────");
            WriteC("» ");

            // Lee la opción elegida por el usuario
            int op = LeerEntero();
            Console.WriteLine();

            switch (op)
            {
                case 1:
                    // ── Submenú de retiros ───────────────────
                    // Muestra montos predefinidos y la opción
                    // de ingresar una cantidad personalizada
                    PrintC("•••••  RETIROS  •••••");
                    PrintC("[1] $50      [2] $100     [3] $150", ConsoleColor.Green);
                    PrintC("[4] $200     [5] $250     [6] $500", ConsoleColor.Green);
                    PrintC("[7] Otra cantidad", ConsoleColor.Green);
                    PrintC("[0] Volver al menú", ConsoleColor.DarkGray);
                    PrintC("────────────────────────────────────────────");
                    WriteC("» ");

                    // Diccionario que relaciona cada opción con su monto
                    var montos = new Dictionary<int, int>
                    {
                        {1,50},{2,100},{3,150},{4,200},{5,250},{6,500}
                    };

                    int opc = LeerEntero();

                    // Si el usuario elige 0, regresa al menú principal
                    if (opc == 0) break;

                    if (montos.TryGetValue(opc, out int monto))
                    {
                        Retirar(monto, db);
                    }
                    else if (opc == 7)
                    {
                        // Cantidad personalizada ingresada por el usuario
                        PrintC("Ingrese la cantidad a retirar:");
                        WriteC("$ ");
                        int cantidad = LeerEntero();
                        Retirar(cantidad, db);
                    }
                    else
                    {
                        PrintC("Opción no válida.", ConsoleColor.Red);
                    }
                    break;

                case 2:
                    // ── Submenú de pago de servicios ─────────
                    // Muestra los servicios disponibles con su costo fijo
                    PrintC("•••••  PAGO DE SERVICIOS  •••••");
                    PrintC("────────────────────────────────────────────");
                    PrintC("[1] Internet   - $350", ConsoleColor.Green);
                    PrintC("[2] Luz        - $500", ConsoleColor.Green);
                    PrintC("[3] Agua       - $200", ConsoleColor.Green);
                    PrintC("[4] Gas        - $300", ConsoleColor.Green);
                    PrintC("[5] Teléfono   - $250", ConsoleColor.Green);
                    PrintC("[6] Televisión - $400", ConsoleColor.Green);
                    PrintC("[0] Volver al menú", ConsoleColor.DarkGray);
                    PrintC("────────────────────────────────────────────");
                    WriteC("» ");

                    // Diccionario que relaciona cada opción con el nombre
                    // del servicio y su costo mensual fijo usando tuplas
                    var servicios = new Dictionary<int, (string nombre, int costo)>
                    {
                        { 1, ("Internet",   350) },
                        { 2, ("Luz",        500) },
                        { 3, ("Agua",       200) },
                        { 4, ("Gas",        300) },
                        { 5, ("Teléfono",   250) },
                        { 6, ("Televisión", 400) }
                    };

                    int opcServicio = LeerEntero();

                    // Si el usuario elige 0, regresa al menú principal
                    if (opcServicio == 0) break;

                    if (servicios.TryGetValue(opcServicio, out var servicio))
                    {
                        // Muestra el resumen del pago antes de confirmar
                        PrintC("────────────────────────────────────────────");
                        PrintC($"  Servicio : {servicio.nombre}", ConsoleColor.Cyan);
                        PrintC($"  Monto    : ${servicio.costo}", ConsoleColor.Cyan);
                        PrintC("────────────────────────────────────────────");
                        PrintC("¿Confirmar pago? (1) Sí   (0) No", ConsoleColor.Yellow);
                        WriteC("» ");

                        int confirmar = LeerEntero();

                        if (confirmar == 1)
                        {
                            // Confirmado: realiza el pago
                            PagarServicio(servicio.nombre, servicio.costo, db);
                        }
                        else
                        {
                            // Cancelado: regresa sin hacer nada
                            PrintC("Pago cancelado.", ConsoleColor.Red);
                        }
                    }
                    else
                    {
                        PrintC("Opción no válida.", ConsoleColor.Red);
                    }
                    break;

                case 3:
                    // ── Submenú de depósitos ─────────────────
                    // Muestra montos predefinidos y la opción
                    // de ingresar una cantidad personalizada
                    PrintC("•••••  DEPÓSITOS  •••••");
                    PrintC($"  (Límite por operación: ${LIMITE_DEPOSITO})", ConsoleColor.DarkGray);
                    PrintC("[1] $50      [2] $100     [3] $150", ConsoleColor.Green);
                    PrintC("[4] $200     [5] $250     [6] $500", ConsoleColor.Green);
                    PrintC("[7] Otra cantidad", ConsoleColor.Green);
                    PrintC("[0] Volver al menú", ConsoleColor.DarkGray);
                    PrintC("────────────────────────────────────────────");
                    WriteC("» ");

                    // Diccionario que relaciona cada opción con su monto
                    var montosDeposito = new Dictionary<int, int>
                    {
                        {1,50},{2,100},{3,150},{4,200},{5,250},{6,500}
                    };

                    int opcDeposito = LeerEntero();

                    // Si el usuario elige 0, regresa al menú principal
                    if (opcDeposito == 0) break;

                    if (montosDeposito.TryGetValue(opcDeposito, out int montoDeposito))
                    {
                        // Monto fijo encontrado en el diccionario
                        Depositar(montoDeposito, db);
                    }
                    else if (opcDeposito == 7)
                    {
                        // Cantidad personalizada ingresada por el usuario
                        PrintC("Ingrese la cantidad a depositar:");
                        WriteC("$ ");
                        int cantidadDeposito = LeerEntero();
                        Depositar(cantidadDeposito, db);
                    }
                    else
                    {
                        PrintC("Opción no válida.", ConsoleColor.Red);
                    }
                    break;

                case 0:
                    // ── Salir del programa ───────────────────
                    // Muestra mensaje de despedida y termina el Main()
                    Console.Clear();
                    PrintC("╔════════════════════════════════════════════╗");
                    PrintC("║         GRACIAS POR USAR EL CAJERO         ║");
                    PrintC("╚════════════════════════════════════════════╝");
                    Console.ResetColor();
                    return;

                default:
                    // Cualquier número fuera de las opciones válidas
                    PrintC("Opción no válida.", ConsoleColor.Red);
                    break;
            }

            // ── Pausa al final de cada acción ────────────────
            // Da tiempo al usuario de leer el resultado
            // antes de que el while limpie la pantalla
            Console.WriteLine();
            PrintC("Presione cualquier tecla para volver al menú...", ConsoleColor.DarkGray);
            Console.ReadKey();
        }
    }
}

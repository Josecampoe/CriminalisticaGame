using System.Runtime.CompilerServices;

// Las herramientas de editor (generador de datos) y las pruebas pueden asignar los datos;
// el código del juego solo los lee.
[assembly: InternalsVisibleTo("Caso404.Editor")]
[assembly: InternalsVisibleTo("Caso404.Tests.EditMode")]
[assembly: InternalsVisibleTo("Caso404.Tests.PlayMode")]

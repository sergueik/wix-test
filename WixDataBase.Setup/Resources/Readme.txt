WIX PARA DESPLIEGUE DE APLICACIONES
Versión: 1.0.0

Creado por Hugo González Olaya
hugo14.gonzalez@gmail.com

Software de libre uso, distribución y modificación.

==================================================================
INSTALAR BASES DE DATOS
==================================================================
El instalador ejecuta archivs scripts de base de datos *.sql en un servidor de base de datos.
Los scripts son copiados en la carpeta de instalación y si lo indica son ejecutados.

Los scripts contiene parámetros los cuales son reemplazados por los datos suministrados por el usuario.
Algunos de los parámetros son:
$(DATABASE_MAILBOX): Buzon de correo usado para configurar el servicio de mail.
$(DATABASE_MAILIP): Dirección IP del servidor de correo.
$(DATABASE_MAILPROFILENAME): Nombre de perfil de correo para configurar el servicio de mail, recomendado el mismo nombre de instancia.
$(DATABASE_OPERATORMAILBOX): Buzones de correo para recibir notificaciones, para el operador: "Operator$(DATABASE_NAME)".
$(DATABASE_PROXYPASSWORD): Contraseña del usuario de Windos usado para crar cuenta proxy.
$(DATABASE_PROXYWINDOWSUSER): Usuario de Windows usado para crar cuenta proxy.
$(DATABASE_NAME): Nombre de la base de datos  
$(DATABASE_PATH_PRIMARY): Ruta archivo principal de base datos *.mdf.
$(DATABASE_PATH_LOG): Ruta archivo log de transacciones *.ldf.

==================================================================
CAMBIAR INSTALACIÓN
==================================================================
El instalador le permite seleccionar las características a ser instaladas.
Si ejecuta de nuevo el instalador puede adicionar características y ejecutarlas en el servidor de base de datos.
Si ejecuta dos veces un script puede presentar errores dependiendo de la forma como creo el script.
Es recomendable que los scripts tenga instrucciones como: IF NOT EXISTS (...) para evitar que la ejecución falle si un objeto existe.
Pero es posible que el objeto no sea actualizado con nueva versión, en este caso es probable que tenga que borrar el objeto antes crear una nueva versión.
Para el caso de creación de tablas tenga cuidado al modificar tablas existentes, porque puede perder los datos.

==================================================================
DESINSTALAR
==================================================================
El proceso de desinstalación no ejecuta scripts de base de datos, ni remueve los objetos de base de datos creados en el proceso de instalación.
Solo borra los elementos creados por el instalador, archivo copiados, claves de registro y accesos directos.
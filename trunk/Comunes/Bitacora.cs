/*
 * Creado por SharpDevelop.
 * Usuario: eplatzer
 * Fecha: 17/04/2008
 * Hora: 13:44
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;

namespace Comunes
{
	/// <summary>
	/// Description of Bitacora.
	/// </summary>
	public class Bitacora
	{
		string nombreArchivoTodosRegistros;
		string nombreArchivoUltimoRegistro;
		public Bitacora()
		{
		}
		public Bitacora(string nombreArchivoUltimoRegistro,string nombreArchivoTodosRegistros){
			string carpeta=Cadena.AgregarSiFalta(System.Environment.GetEnvironmentVariable("TEMP"),@"\");
			this.nombreArchivoTodosRegistros=carpeta+nombreArchivoTodosRegistros;
			this.nombreArchivoUltimoRegistro=carpeta+nombreArchivoUltimoRegistro;
			if(nombreArchivoTodosRegistros!=null){
				Archivo.Borrar(nombreArchivoTodosRegistros);
			}
		}
		public T Registrar<T>(T mensaje){
			if(nombreArchivoUltimoRegistro!=null){
				Archivo.Escribir(nombreArchivoUltimoRegistro,mensaje.ToString());
			}
			if(nombreArchivoTodosRegistros!=null){
				Archivo.Agregar(nombreArchivoTodosRegistros,Cadena.AgregarSiFalta(mensaje.ToString(),"\n"));
			}
			return mensaje;
		}
		public string RegistrarSql(string sentencia){
			Registrar(sentencia);
			return sentencia;
		}
	}
}

/*
 * Creado por SharpDevelop.
 * Usuario: eplatzer
 * Fecha: 17/04/2008
 * Hora: 13:44
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificaci�n | Editar Encabezados Est�ndar
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
			if(this.nombreArchivoTodosRegistros!=null){
				Archivo.Borrar(this.nombreArchivoTodosRegistros);
			}
		}
		public string prefijo(){
			return "-- "+DateTime.Now.Hour+":"+DateTime.Now.Minute+":"+DateTime.Now.Second+Archivo.Eol;
		}
		public T Registrar<T>(T mensaje){
			if(nombreArchivoUltimoRegistro!=null){
				Archivo.Escribir(nombreArchivoUltimoRegistro,prefijo()+mensaje.ToString());
			}
			if(nombreArchivoTodosRegistros!=null){
				Archivo.Agregar(nombreArchivoTodosRegistros,prefijo()+Cadena.AgregarSiFalta(mensaje.ToString(),"\n"));
			}
			return mensaje;
		}
		public T RegistrarAdicional<T>(T mensaje){
			if(nombreArchivoUltimoRegistro!=null){
				Archivo.Agregar(nombreArchivoUltimoRegistro,prefijo()+mensaje.ToString());
			}
			if(nombreArchivoTodosRegistros!=null){
				Archivo.Agregar(nombreArchivoTodosRegistros,prefijo()+Cadena.AgregarSiFalta(mensaje.ToString(),"\n"));
			}
			return mensaje;
		}
		public string RegistrarSql(string sentencia){
			Registrar(sentencia);
			return sentencia;
		}
	}
}

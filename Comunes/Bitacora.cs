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
	public class Bitacora
	{
		string nombreArchivoTodosRegistros;
		string nombreArchivoUltimoRegistro;
		DateTime tickAnterior;
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
			string rta="-- "
				+DateTime.Now.ToLongTimeString()
				+(tickAnterior==null?""
				  :"-"+tickAnterior.ToLongTimeString()+"="+(DateTime.Now-tickAnterior).ToString()
				 )
				+Archivo.Eol;
			tickAnterior=DateTime.Now.AddDays(1);
			tickAnterior=tickAnterior.AddDays(-1);
			return rta;
		}
		public T Registrar<T>(T mensaje){
			string prefijo=this.prefijo();
			if(nombreArchivoUltimoRegistro!=null){
				Archivo.Escribir(nombreArchivoUltimoRegistro,prefijo+mensaje.ToString());
			}
			if(nombreArchivoTodosRegistros!=null){
				Archivo.Agregar(nombreArchivoTodosRegistros,prefijo+Cadena.AgregarSiFalta(mensaje.ToString(),"\n"));
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

/*
 * Creado por SharpDevelop.
 * Usuario: Emilio
 * Fecha: 30/03/2008
 * Hora: 20:24
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.IO;

namespace TodoASql
{
	/// <summary>
	/// Description of Carpeta.
	/// </summary>
	public delegate bool ProcesadorPlanoContestaSiPudo(string contenidoPlano);
	public delegate bool ProcesadorArchivoContestaSiPudo(string nombreArchivo);
		
	public class Carpeta
	{
		string Directorio;
		public Carpeta(string Directorio){
			this.Directorio=Directorio;
		}
		public void ProcesarArchivos(string nombres,string nuevaExtension,ProcesadorArchivoContestaSiPudo procesar){
			DirectoryInfo dir=new DirectoryInfo(Directorio);
			FileInfo[] archivos=dir.GetFiles(nombres);
			foreach(FileInfo archivo in archivos){
				System.Console.Write("Archivo "+archivo.FullName);
				if(procesar(archivo.FullName)){
					System.Console.WriteLine(" procesado");
					File.Delete(archivo.FullName+"."+nuevaExtension);
					File.Move(archivo.FullName,archivo.FullName+"."+nuevaExtension);
				}else{
					System.Console.WriteLine(" ERROR NO SE PUEDE PROCESAR");
				}
			}
		}
		public void ProcesarArchivosPlanos(string nombres,string nuevaExtension,ProcesadorPlanoContestaSiPudo procesar){
			ProcesarArchivos(nombres,nuevaExtension,
			                 delegate(string nombreArchivo){
				string contenidoPlano=Archivo.Leer(nombreArchivo);
				System.Console.Write(" leido");
				return procesar(contenidoPlano);
			                 }
			);
		}
	}
}

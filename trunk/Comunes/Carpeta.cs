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

namespace Comunes
{
	public delegate bool ProcesadorPlanoContestaSiPudo(string contenidoPlano);
	public delegate bool ProcesadorArchivoContestaSiPudo(string nombreArchivo);
		
	public class Carpeta
	{
		string Directorio;
		public Carpeta(string Directorio){
			this.Directorio=Directorio;
		}
		void queNoExista(string nombreArchivo){
		reintentar:
			if(Archivo.Existe(nombreArchivo)){
				System.Windows.Forms.DialogResult rta=
					System.Windows.Forms.MessageBox.Show(
						"El archivo "+nombreArchivo+" existe. No debería existir (para no confundir cada archivo solo puede estar en una de las tres carpetas: a procesar, procesados o salteados"
						,"Atención"
						,System.Windows.Forms.MessageBoxButtons.AbortRetryIgnore
						,System.Windows.Forms.MessageBoxIcon.Warning
						,System.Windows.Forms.MessageBoxDefaultButton.Button3);
				if(rta==System.Windows.Forms.DialogResult.Abort){
					Falla.Detener("Existen un archivo a procesar que tiene una replicación en "+nombreArchivo);
				}
				if(rta==System.Windows.Forms.DialogResult.Retry){
					goto reintentar;
				}
				if(rta==System.Windows.Forms.DialogResult.Ignore){
					rta=
						System.Windows.Forms.MessageBox.Show(
							"¿Borro el archivo "+nombreArchivo+"?"
							,"Atención CONFIRME EL BORRADO DE ARCHIVO"
							,System.Windows.Forms.MessageBoxButtons.YesNo
							,System.Windows.Forms.MessageBoxIcon.Stop
							,System.Windows.Forms.MessageBoxDefaultButton.Button2);
					if(rta==System.Windows.Forms.DialogResult.Yes){
						Archivo.Borrar(nombreArchivo);
					}
					if(rta==System.Windows.Forms.DialogResult.No){
						goto reintentar;
					}
				}
			}
		}
		public void ProcesarArchivos(string nombres,string subcarpetaProcesados,string subcarpetaSalteados,ProcesadorArchivoContestaSiPudo procesar){
			DirectoryInfo dir=new DirectoryInfo(Directorio);
			FileInfo[] archivos=dir.GetFiles(nombres);
			foreach(FileInfo archivo in archivos){
				string nombreProcesado=archivo.DirectoryName+@"\"+subcarpetaProcesados+@"\"+archivo.Name;
				string nombreSalteado=archivo.DirectoryName+@"\"+subcarpetaSalteados+@"\"+archivo.Name;
				System.Console.Write("Archivo "+archivo.FullName);
				queNoExista(nombreProcesado);
				queNoExista(nombreSalteado);
				if(procesar(archivo.FullName)){
					System.Console.WriteLine(" procesado");
					Archivo.RenombrarMover(archivo.FullName,nombreProcesado);
				}else{
					System.Console.WriteLine(" ERROR NO SE PUEDE PROCESAR");
					Archivo.RenombrarMover(archivo.FullName,nombreSalteado);
				}
			}
		}
		public void ProcesarArchivosPlanos(string nombres,string subcarpetaProcesados,string subcarpetaSalteados,ProcesadorArchivoContestaSiPudo procesar){
			ProcesarArchivos(nombres,subcarpetaProcesados,subcarpetaSalteados,
			    delegate(string nombreArchivo)
			    {
					string contenidoPlano=Archivo.Leer(nombreArchivo);
					System.Console.Write(" leido");
					return procesar(contenidoPlano);
			    }
			);
		}
	}
}

/*
 * Creado por SharpDevelop.
 * Usuario: Administrador
 * Fecha: 16/02/2008
 * Hora: 12:19 p.m.
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Comunes
{
	public delegate void Accion();
	public class Archivo{
		public const string Eol="\r\n";
		public static string Leer(string nombreArchivo){
			StreamReader re = File.OpenText(nombreArchivo);
			string rta=re.ReadToEnd();
			re.Close();
			return rta;
		}
		public static void Escribir(string nombreArchivo, string contenido){
			StreamWriter re=null;
			tratarDe("escribir en el archivo "+nombreArchivo
			         ,"quizás el archivo "+nombreArchivo+" esté abierto y por eso el programa no lo puede pisar"
			         ,delegate(){
				re = File.CreateText(nombreArchivo);
			});
			re.Write(contenido);
			re.Close();
		}
		public static void Agregar(string nombreArchivo, string contenido){
			StreamWriter re=null;
			tratarDe("agregar datos en el archivo "+nombreArchivo
			         ,"quizás el archivo "+nombreArchivo+" esté abierto y por eso el programa no le puede agregar datos"
			         ,delegate(){
				re = File.AppendText(nombreArchivo);
			});
			re.Write(contenido);
			re.Close();
		}
		public static void tratarDe(string descripcionAccion,string quizasSeExpiquePor,Accion accion){
		reintentar:
			string titulo="Atención";
			var icono=System.Windows.Forms.MessageBoxIcon.Exclamation;
			try{
				accion();
			}catch(System.IO.IOException ex){
			volverMostrarCartel:
				System.Windows.Forms.DialogResult rta=
					System.Windows.Forms.MessageBox.Show(
						"No se pudo "+descripcionAccion
							+" ("+ex.Message+")"+Eol+quizasSeExpiquePor
						,titulo
						,System.Windows.Forms.MessageBoxButtons.AbortRetryIgnore
						,icono
						,System.Windows.Forms.MessageBoxDefaultButton.Button3
					);
				if(rta==System.Windows.Forms.DialogResult.Retry){
					goto reintentar;
				}
				if(rta==System.Windows.Forms.DialogResult.Abort){
					throw;
				}
				if(rta==System.Windows.Forms.DialogResult.Ignore){
					titulo="Atención: NO SE PUEDE IGNORAR NI OMITIR ESTE MENSAJE";
					icono=System.Windows.Forms.MessageBoxIcon.Hand;
					goto volverMostrarCartel;
				}
			}
		}
		public static void Borrar(string nombreArchivo){
			tratarDe("borrar "+nombreArchivo
			         ,"quizás el archivo esté abierto por otro programa"
			         ,delegate(){
				File.Delete(nombreArchivo);
			});
		}
		public static bool Existe(string nombreArchivo){
			return File.Exists(nombreArchivo);
		}
		public static string CarpetaActual(){
			return Directory.GetCurrentDirectory();
		}
		public static void Copiar(string ArchivoViejo,string ArchivoNuevo){
			tratarDe("copiar el archivo "+ArchivoViejo+" en "+ArchivoNuevo
			         ,"quizás el archivo "+ArchivoViejo+" exista, en ese caso hay que borrarlo manualmente porque el programa no lo hace"
			         ,delegate()
			{
				File.Copy(ArchivoViejo,ArchivoNuevo,false);
			});
		}
		public static void CopiarPisando(string ArchivoViejo,string ArchivoNuevo){
			tratarDe("copiar el archivo "+ArchivoViejo+" en "+ArchivoNuevo+" (pisando éste si existe)"
			         ,"quizás el archivo "+ArchivoViejo+" esté abierto y por eso el programa no lo puede pisar"
			         ,delegate()
			{
				File.Copy(ArchivoViejo,ArchivoNuevo,true);
			});
		}
		public static void RenombrarMover(string ArchivoViejo,string ArchivoNuevo){
			tratarDe("renombrar o mover el archivo "+ArchivoViejo+" en "+ArchivoNuevo+""
			         ,"quizás el archivo "+ArchivoViejo+" esté abierto y por eso el programa puede renombrar o mover, "+
			          "quizás el archivo "+ArchivoNuevo+" exista y por eso no se puede mover o renombrar el otro encima"
			         ,delegate()
			{
				File.Move(ArchivoViejo,ArchivoNuevo);
			});
		}
			
	}
}

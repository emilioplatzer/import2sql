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
	public class Archivo{
		public static string Leer(string nombreArchivo){
			StreamReader re = File.OpenText(nombreArchivo);
			string rta=re.ReadToEnd();
			re.Close();
			return rta;
		}
		public static void Escribir(string nombreArchivo, string contenido){
			StreamWriter re = File.CreateText(nombreArchivo);
			re.Write(contenido);
			re.Close();
		}
		public static void Agregar(string nombreArchivo, string contenido){
			StreamWriter re = File.AppendText(nombreArchivo);
			re.Write(contenido);
			re.Close();
		}
		public static void Borrar(string nombreArchivo){
			File.Delete(nombreArchivo);
		}
		public static bool Existe(string nombreArchivo){
			return File.Exists(nombreArchivo);
		}
		public static string CarpetaActual(){
			return Directory.GetCurrentDirectory();
		}
		public static void Copiar(string ArchivoViejo,string ArchivoNuevo){
			File.Copy(ArchivoViejo,ArchivoNuevo,false);
		}
		public static void CopiarPisando(string ArchivoViejo,string ArchivoNuevo){
			File.Copy(ArchivoViejo,ArchivoNuevo,true);
		}
	}
}

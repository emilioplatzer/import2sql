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
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace TodoASql
{
	/// <summary>
	/// Description of Otras.
	/// </summary>
	public class Otras
	{
		public static int cantidadOcurrencias(char caracterBuscado, string lugarDondeBuscar){
			int i=0,rta=0;
			while((i=lugarDondeBuscar.IndexOf(caracterBuscado,i))>=0){
				i++; rta++;
			}
			return rta;
		}
		/// <summary>
		/// Verdadero si la cantidad de "{" es igual a la de"}"
		/// </summary>
		public static bool llavesBalanceadas(string s){
			return cantidadOcurrencias('{',s)==cantidadOcurrencias('}',s);
		}
		/// <summary>
		/// Devuelve un string sacando acentos a las vocales
		/// </summary>
		public static string sinAcentos(string frase){
			return frase.Replace("á","a")
				.Replace("é","e")
				.Replace("í","i")
				.Replace("ó","o")
				.Replace("ú","u");
			/* Hoy aprendimos:
			 * Que no hay que hacer nada que dependa de la "localidad" de la maquina
			 * En una máquina en ruso esto no funcionaba:
			return Encoding.ASCII.GetString(
					Encoding.GetEncoding(1251).GetBytes(frase)
				).ToLower();
			*/
		}
		public static string leerArchivoCompleto(string nombreArchivo){
			StreamReader re = File.OpenText(nombreArchivo);
			string rta=re.ReadToEnd();
			re.Close();
			return rta;
		}
		public static string mayusculaPrimeraLetra(string s){
			return s.Substring(0,1).ToUpper()+s.Substring(1).ToLower();
		}
		public static void escribirArchivo(string nombreArchivo, string contenido){
			StreamWriter re = File.CreateText(nombreArchivo);
			re.Write(contenido);
			re.Close();
		}
		public static void agregarArchivo(string nombreArchivo, string contenido){
			StreamWriter re = File.AppendText(nombreArchivo);
			re.Write(contenido);
			re.Close();
		}
		public static void borrarArchivo(string nombreArchivo){
			File.Delete(nombreArchivo);
		}
		public static bool existeArchivo(string nombreArchivo){
			return File.Exists(nombreArchivo);
		}
		public static int min(int uno,int dos){
			return uno<dos?uno:dos;
		}
		public static void ShowArray(Array theArray) {
	        foreach (Object o in theArray) {
	            Console.Write("[{0}]", o);
	        }
	        Console.WriteLine("\n");
	    }

		public static string expandirSignoIgual(string s){
			int i,caracterNumerico;
			char c;
			string digito="0123456789ABCDEF";
			i=-1;
			while(true){
				i=s.IndexOf('=',i+1);
			if(i<=0) break;
				if(s[i+1]=='\n'){
					s=s.Remove(i,1);
				}else{
					caracterNumerico=digito.IndexOf(s[i+1])*16+digito.IndexOf(s[i+2]);
					try{
						c=(char)(caracterNumerico);
						s=s.Substring(0,i)+c+s.Substring(i+3);
					}catch(System.OverflowException){
						i++;
					}
				}
			}
			return s;
		}
	}
	
	/// <summary>
	/// Para iterar en un loop foreach con los sufijos de texto Padre e Hijo
	/// También se puede iterar sobre Padre e Hijo o Nada en función de un parámetro booleano
	///    de ese modo el loop se ejecuta dos veces con Padre, Hijo o una con "" 
	/// </summary>
	public class PadreHijo{
		enum Posibilidades {Nada=0, Hijo=1, Padre=2}
		Posibilidades soy;
		PadreHijo(Posibilidades loQueSere){ soy=loQueSere; }
		public static List<PadreHijo> Ambos(){
			List<PadreHijo> rta=new List<PadreHijo>();
			rta.Add(new PadreHijo(Posibilidades.Hijo));
			rta.Add(new PadreHijo(Posibilidades.Padre));
			return rta;
		}
		public static List<PadreHijo> AmbosSiTrue_NadaSiFalse(bool b){
			if(b){
				return Ambos();
			}else{
				List<PadreHijo> rta=new List<PadreHijo>();
				rta.Add(new PadreHijo(Posibilidades.Nada));
				return rta;
			}
		}
		public override string ToString(){
			switch(soy){
				case Posibilidades.Nada: return "";
				case Posibilidades.Hijo: return "Hijo";
				case Posibilidades.Padre: return "Padre";
				default : return "";
			}
		}
		public string ToLower(){
			return ToString().ToLower();
		}
		public PadreHijo otro(){
			switch(soy){
				case Posibilidades.Hijo: return new PadreHijo(Posibilidades.Padre);
				case Posibilidades.Padre: return new PadreHijo(Posibilidades.Hijo);
				default: return this;
			}
		}
	}
}

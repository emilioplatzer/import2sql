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
using NUnit.Framework;

namespace TodoASql
{
	/// <summary>
	/// Description of Otras.
	/// </summary>
	public class Otras
	{
		public static int Min(int uno,int dos){
			return uno<dos?uno:dos;
		}
		public static void ShowArray(Array theArray) {
	        foreach (Object o in theArray) {
	            Console.Write("[{0}]", o);
	        }
	        Console.WriteLine("\n");
	    }
	}
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
	}
	
	public class Cadena{
		public static int CantidadOcurrencias(char caracterBuscado, string lugarDondeBuscar){
			int i=0,rta=0;
			while((i=lugarDondeBuscar.IndexOf(caracterBuscado,i))>=0){
				i++; rta++;
			}
			return rta;
		}
		/// <summary>
		/// Verdadero si la cantidad de "{" es igual a la de"}"
		/// </summary>
		public static bool LlavesBalanceadas(string s){
			return CantidadOcurrencias('{',s)==CantidadOcurrencias('}',s);
		}
		/// <summary>
		/// Devuelve un string sacando acentos a las vocales
		/// </summary>
		public static string SacarAcentos(string frase){
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
		public static string MayusculaPrimeraLetra(string s){
			return s.Substring(0,1).ToUpper()+s.Substring(1).ToLower();
		}
		public static string ExpandirSignoIgual(string s){
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
		public static string SacarComillas(string valor){
			return valor.Replace('"',' ')
				.Replace('\n',' ')
				.Replace('\r',' ')
				.Replace('\t',' ')
				.Substring(0,Otras.Min(250,valor.Length)).Trim();
		}
	}
	[TestFixture]
	public class ProbarCadena{
		public ProbarCadena(){
		}
		[Test]
		public void CantidadOcurrencias(){
			Assert.AreEqual(0,Cadena.CantidadOcurrencias('i',"hola como andás?"));
			Assert.AreEqual(3,Cadena.CantidadOcurrencias('o',"hola como andás?"));
			Assert.IsFalse(Cadena.LlavesBalanceadas("{{{no}no{no"),"no");
			Assert.IsTrue(Cadena.LlavesBalanceadas("{{{no}no{no}}}si"),"si");
		}
		[Test]
		public void SacarAcentos(){
			Assert.AreEqual("hola como andas?",Cadena.SacarAcentos("hola como andás?"));
		}
		[Test]
		public void SignoIgual(){
			Assert.AreEqual("hola che",Cadena.ExpandirSignoIgual("hola=20che"));
			Assert.AreEqual("Región",Cadena.ExpandirSignoIgual("Regi=F3n"));
			Assert.AreEqual("Línea",Cadena.ExpandirSignoIgual("L=EDnea"));
			Assert.AreEqual("el \nsalto",Cadena.ExpandirSignoIgual("el =\nsalto"));
			Assert.AreEqual("lang=ES",Cadena.ExpandirSignoIgual("lang=3DES"));
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
		public PadreHijo Otro(){
			switch(soy){
				case Posibilidades.Hijo: return new PadreHijo(Posibilidades.Padre);
				case Posibilidades.Padre: return new PadreHijo(Posibilidades.Hijo);
				default: return this;
			}
		}
	}
}

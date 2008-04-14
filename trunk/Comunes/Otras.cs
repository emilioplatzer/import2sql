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
	public class Objeto{
		public static string ExpandirMiembros(Object o,int identacion){
			if(o==null){
				return "null";
			}else if(o.GetType().Name=="String" || o.GetType().Name=="string"){
				return '"'+o.ToString()+'"';
			}else if(o.GetType().IsValueType){
				return o.ToString();
			}else{
				int anchoTab=3;
				StringBuilder rta=new StringBuilder();
				rta.AppendLine(o.GetType().Name+"{");
				string margen=new string(' ',(identacion+1)*anchoTab);
				FieldInfo[] fs=o.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
				foreach(FieldInfo f in fs){
					Object objetoValor=f.GetValue(o);
					rta.AppendLine(margen+f.Name+":"+ExpandirMiembros(objetoValor,identacion+1));
				}
				rta.AppendLine(new string(' ',identacion*anchoTab)+"}");
				return rta.ToString();
			}
		}
		public static string ExpandirMiembros(Object o){
			return ExpandirMiembros(o,0);
		}
		public static string[] Paratodo(string[] vector,DelegateString_String f){
			string[] rta=new string[vector.Length];
			for(int i=0;i<vector.Length;i++){
				rta[i]=f(vector[i]);
			}
			return rta;
		}
	}
	[TestFixture]
	public class ProbarObjeto{
		[Test]
		public void ExpandirMiembros(){
			ParametrosPrueba pNO=new ParametrosPrueba(ParametrosPrueba.LeerPorDefecto.NO);
			Assert.AreEqual("ParametrosPrueba{\r\n   DirUno:null\r\n   Frase:null\r\n   Cantidad:0\r\n   Fecha:01/01/0001 0:00:00\r\n}\r\n",Objeto.ExpandirMiembros(pNO));
			ParametrosPrueba pSI=new ParametrosPrueba(ParametrosPrueba.LeerPorDefecto.SI);
			System.Console.WriteLine(Objeto.ExpandirMiembros(pSI));
			Assert.AreEqual("ParametrosPrueba{\r\n   DirUno:\"c:\\temp\\aux\"\r\n   Frase:\"No hay futuro\"\r\n   Cantidad:-1\r\n   Fecha:01/02/2003 0:00:00\r\n}\r\n",Objeto.ExpandirMiembros(pSI));
			// Assert.Ignore("Ojo que esto falla la primera vez que se usa");
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
	public class UnSoloUso{
		int usos=0;
		public void Uso(){
			usos++;
			if(usos>1){
				throw new System.Exception("Solo se puede usar una vez");
			}
		}
	}
	public class Controlar{
		static string etiquetas;
		static Controlar(){
			string nombreArchivo="ControlarEtiquetas.txt";
			if(Archivo.Existe(nombreArchivo)){
				etiquetas=";"+Archivo.Leer(nombreArchivo)+";";
			}else{
				etiquetas=";";
			}
		}
		public static bool EstaDefinido(string etiqueta){
			return etiquetas.IndexOf(";"+etiqueta+";")>=0;
		}
		public static void Definido(string etiqueta){
			if(!EstaDefinido(etiqueta)){
				Assert.Fail("Sobra la etiqueta "+etiqueta);
			}
		}
		public static void NoDefinido(string etiqueta){
			if(EstaDefinido(etiqueta)){
				Assert.Fail("Falta la etiqueta "+etiqueta);
			}
		}
	}
	[TestFixture]
	public class PrMiEntendimiento{
		[Test]
		public void Colecciones(){
			System.Collections.Generic.List<string> Lista=new System.Collections.Generic.List<string>();
			Lista.Add("Hola");
			Lista.Add("Que");
			System.Collections.Generic.List<string> OtraLista=new System.Collections.Generic.List<string>();
			OtraLista.AddRange(Lista);
			Lista.Add("Tal");
			Assert.AreEqual(3,Lista.Count);
			Assert.AreEqual(2,OtraLista.Count);
		}
	}
}

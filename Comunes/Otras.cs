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
	/*
	public interface IBastanteComparable{
		
	}
	*/
	public class Otras
	{
		public static int Min(int uno,int dos){
			return uno<dos?uno:dos;
		}
		public static Nullable<T> MAX<T>(Nullable<T> uno,Nullable<T> dos) where T:struct,IComparable{
			if(uno.HasValue){
				if(dos.HasValue){
					return uno.Value.CompareTo(dos.Value)>0?uno:dos;
				}else{
					return uno;
				}
			}else{
				return dos;
			}
		}
		public static Nullable<T> MIN<T>(Nullable<T> uno,Nullable<T> dos) where T:struct,IComparable{
			if(uno.HasValue){
				if(dos.HasValue){
					return uno.Value.CompareTo(dos.Value)<0?uno:dos;
				}else{
					return uno;
				}
			}else{
				return dos;
			}
		}
		public static T Max<T>(T uno,T dos) where T:IComparable{
			return uno.CompareTo(dos)>0?uno:dos;
		}
		public static void ShowArray(Array theArray) {
	        foreach (Object o in theArray) {
	            Console.Write("[{0}]", o);
	        }
	        Console.WriteLine("\n");
	    }
	}
	public class Objeto{
		const bool condebug=false;
		private static string debug(string frase){
			#pragma warning disable 162
			if(condebug){
				System.Console.Write(frase);
			}
			#pragma warning restore 162
			return frase;
		}
		private static string ExpandirMiembros<T>(Conjunto<Object> vistos,Conjunto<T> conj,int identacion,bool comprimirLineas){
			string FinLinea;
			if(comprimirLineas){
				FinLinea="";
			}else{
				FinLinea="\n";
			}
			int anchoTab=3;
			string margen=new string(' ',(identacion+1)*anchoTab);
			StringBuilder rta=new StringBuilder();
			foreach(T objetoValor in conj.Keys){
				rta.Append(debug(margen+"+"+ExpandirMiembros(vistos,objetoValor,identacion+1,comprimirLineas)+FinLinea));
			}
			return rta.ToString();
		}
		private static string ExpandirMiembros(Conjunto<Object> vistos,Object o,int identacion,bool comprimirLineas){
			string FinLinea;
			if(comprimirLineas){
				FinLinea="";
			}else{
				FinLinea="\n";
			}
			if(o!=null){
				if(vistos.Contiene(o)){
					return "#"+FinLinea;
				} 
				vistos.Add(o);
			}
			if(o==null){
				return "null";
			}else if(o.GetType().Name=="String" || o.GetType().Name=="string"){
				return '"'+o.ToString()+'"';
			}else if(o.GetType().IsValueType){
				return o.ToString();
			}else{
				int anchoTab=3;
				StringBuilder rta=new StringBuilder();
				string margen=new string(' ',(identacion+1)*anchoTab);
				if(o.GetType().IsArray){
					rta.Append(debug(o.GetType().Name+"=["+FinLinea));
					object[] arreglo=(object[]) o;
					int posicion=0;
					foreach(object elemento in arreglo){
						rta.Append(debug(margen+posicion+":"+ExpandirMiembros(vistos,elemento,identacion+1,comprimirLineas)+FinLinea));
						posicion++;
					}
					rta.Append(debug(new string(' ',identacion*anchoTab)+"]"+FinLinea));
				}else{
					rta.Append(debug(o.GetType().Name+"{"+FinLinea));
					FieldInfo[] fs=o.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
					foreach(FieldInfo f in fs){
						Object objetoValor=f.GetValue(o);
						rta.Append(debug(margen+f.Name+":"+ExpandirMiembros(vistos,objetoValor,identacion+1,comprimirLineas)+FinLinea));
					}
					rta.Append(debug(new string(' ',identacion*anchoTab)+"}"+FinLinea));
				}
				return rta.ToString();
			}
		}
		public static string ExpandirMiembros(Object o){
			return ExpandirMiembros(new Conjunto<Object>(),o,0,false);
		}
		private static string ExpandirTodo(Object o,int identacion){
			if(o==null){
				return "null";
			}else if(o.GetType().Name=="String" || o.GetType().Name=="string"){
				return '"'+o.ToString()+'"';
			}else if(o.GetType().IsValueType){
				return o.ToString();
			}else{
				int anchoTab=3;
				StringBuilder rta=new StringBuilder();
				string margen=new string(' ',(identacion+1)*anchoTab);
				if(o.GetType().IsArray){
					rta.AppendLine(o.GetType().Name+"=[");
					object[] arreglo=(object[]) o;
					int posicion=0;
					foreach(object elemento in arreglo){
						rta.AppendLine(margen+posicion+":"+ExpandirTodo(elemento,identacion+1));
						posicion++;
					}
					rta.AppendLine(new string(' ',identacion*anchoTab)+"]");
				}else{
					rta.AppendLine(o.GetType().Name+"{");
					FieldInfo[] fs=o.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
					foreach(FieldInfo f in fs){
						Object objetoValor=f.GetValue(o);
						rta.AppendLine(margen+f.Name+":"+ExpandirTodo(objetoValor,identacion+1));
					}
					PropertyInfo[] ps=o.GetType().GetProperties();
					foreach(PropertyInfo p in ps){
						// MethodInfo m=p.GetGetMethod();
						#pragma warning disable 168
						try{
							if(p.GetIndexParameters().Length==0){
								rta.AppendLine(margen+p.Name+"="+ExpandirMiembros(new Conjunto<Object>(),p.GetValue(o,new object[0]{}),identacion+1,true));
							}
						}catch(System.Exception ex){
						}
						#pragma warning restore 168
					}
					rta.AppendLine(new string(' ',identacion*anchoTab)+"}");
				}
				return rta.ToString();
			}
		}
		public static string ExpandirTodo(Object o){
			return ExpandirTodo(o,0);
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
			Assert.AreEqual("ParametrosPrueba{\n   DirUno:null\n   Frase:null\n   Cantidad:0\n   Fecha:01/01/0001 0:00:00\n}\n",Objeto.ExpandirMiembros(pNO));
			ParametrosPrueba pSI=new ParametrosPrueba(ParametrosPrueba.LeerPorDefecto.SI);
			Assert.AreEqual("ParametrosPrueba{\n   DirUno:\"c:\\temp\\aux\"\n   Frase:\"No hay futuro\"\n   Cantidad:-1\n   Fecha:01/02/2003 0:00:00\n}\n",Objeto.ExpandirMiembros(pSI));
			// Assert.Ignore("Ojo que esto falla la primera vez que se usa");
			string[] frases={"hola", "che"};
			Assert.AreEqual(Cadena.Simplificar("String[]=[0:\"hola\" 1:\"che\"]"),Cadena.Simplificar(Objeto.ExpandirMiembros(frases)));
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
		public const double DeltaDouble=0.000000000001;
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
				Falla.Detener("Sobra la etiqueta "+etiqueta);
			}
		}
		public static void NoDefinido(string etiqueta){
			if(EstaDefinido(etiqueta)){
				Falla.Detener("Falta la etiqueta "+etiqueta);
			}
		}
	}
	[TestFixture]
	public class PrMiEntendimiento{
		enum Dias{Domingo,Lunes,Martes,Miercoles,Jueves,Viernes,Sabado};
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
		[Test]
		public void prEnum(){
			Assert.AreEqual("Martes",Dias.GetName(typeof(Dias),Dias.Martes));
			Assert.AreEqual("Miercoles",Dias.Miercoles.ToString());
			Assert.AreEqual(Dias.Jueves,Dias.Parse(typeof(Dias),"Jueves"));
		}
	}
}

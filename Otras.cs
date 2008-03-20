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
	}
	public class Cadena{
		public static System.Globalization.NumberFormatInfo FormatoPuntoDecimal;
		static Cadena(){
			FormatoPuntoDecimal=new System.Globalization.NumberFormatInfo();
			FormatoPuntoDecimal.NumberDecimalSeparator=".";
		}
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
		public static string ParaSql(object dato){
			if(dato==null){
				return "null";
			}
			if(dato.GetType()==typeof(String)){
				return '"'+SacarComillas((string) dato)+'"';
			}else if(dato.GetType()==typeof(double)){
				return ((double) dato).ToString(Cadena.FormatoPuntoDecimal);
			}else if(dato.GetType()==typeof(DateTime)){
				return '"'+dato.ToString()+'"';
			}else{
				return dato.ToString();
			}
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
			Assert.AreEqual(@"La región",Cadena.ExpandirSignoIgual("La regi=F3n"));
			Assert.AreEqual("Región",Cadena.ExpandirSignoIgual("Regi=F3n"));
			Assert.AreEqual("Línea",Cadena.ExpandirSignoIgual("L=EDnea"));
			Assert.AreEqual("el \nsalto",Cadena.ExpandirSignoIgual("el =\nsalto"));
			Assert.AreEqual("lang=ES",Cadena.ExpandirSignoIgual("lang=3DES"));
		}
		[Test]
		public void RegexSplit(){
			string[] varios=
				Regex.Split("cero=0\nuno=1\ndos=2","\n");
			Assert.AreEqual("uno=1",varios[1]);
		}
		[Test] 
		public void Conversiones(){
			double pi=3.14;
			System.Globalization.NumberFormatInfo formato=new System.Globalization.NumberFormatInfo();
			formato.NumberDecimalSeparator=".";
			Assert.AreEqual("3.14",pi.ToString(formato));
			Assert.AreEqual("3.14",pi.ToString(Cadena.FormatoPuntoDecimal));
			pi+=1002000; // un millón dos mil
			Assert.AreEqual("1002003.14",pi.ToString(Cadena.FormatoPuntoDecimal));
		}
	}
	public class ConjuntosString{
		public static int Cardinal(string conjunto,string separador){
			if(conjunto==null){
				return 0;
			}
			if(conjunto==""){
				return 0;
			}
			int cantidad=0;
			int posicion=0;
			while(true){
				posicion=conjunto.IndexOf(separador,posicion);
			if(posicion<0) break;
				cantidad++;
				posicion+=separador.Length;
			}
			return cantidad+1;
		}
		public static string Elemento(string conjunto,int cual,string separador){
			int posicionInicial;
			int posicionFinal;
			posicionInicial=-1;
			while(true){
				posicionFinal=conjunto.IndexOf(separador,posicionInicial+separador.Length);
				if(posicionFinal<0){
					if(cual==1){
						return conjunto.Substring(posicionInicial+separador.Length);
					}else{
						return null;
					}
				}else{
					if(cual==1){
						return conjunto.Substring(posicionInicial+separador.Length,posicionFinal-posicionInicial-separador.Length);
					}
				}
				cual--;
				posicionInicial=posicionFinal;
			}
		}
	}
	[TestFixture]
	public class ProbarConjuntoString{
		[Test]
		public void Elemento(){
			Assert.AreEqual("hola",ConjuntosString.Elemento("hola;che",1,";"));
			Assert.AreEqual("che",ConjuntosString.Elemento("hola;che;como;andas",2,";"));
			Assert.AreEqual("andas",ConjuntosString.Elemento("hola;che;como;andas",4,";"));
			Assert.AreEqual(null,ConjuntosString.Elemento("hola;che;como;andas",5,";"));
			Assert.AreEqual(null,ConjuntosString.Elemento("",2,";"));
			Assert.AreEqual("che",ConjuntosString.Elemento("hola--che--como--andas",2,"--"));
			Assert.AreEqual("andas",ConjuntosString.Elemento("hola--che--como--andas",4,"--"));
			Assert.AreEqual("andas",ConjuntosString.Elemento("hola----como--andas",4,"--"));
		}
		[Test]
		public void Cardinal(){
			Assert.AreEqual(0,ConjuntosString.Cardinal(null,","));
			Assert.AreEqual(0,ConjuntosString.Cardinal("",","));
			Assert.AreEqual(3,ConjuntosString.Cardinal("hola&che&chau","&"));
			Assert.AreEqual(1,ConjuntosString.Cardinal("hola","&"));
			Assert.AreEqual(3,ConjuntosString.Cardinal("hola<>che<>basta","<>"));
			Assert.AreEqual(3,ConjuntosString.Cardinal("hola,,che",","));
			Assert.AreEqual(4,ConjuntosString.Cardinal("hola...hola......che","..."));
			Assert.AreEqual(4,ConjuntosString.Cardinal("hola...hola.......che","..."));
			Assert.AreEqual(4,ConjuntosString.Cardinal("hola...hola........che","..."));
			Assert.AreEqual(5,ConjuntosString.Cardinal("hola...hola.........che","..."));
		}
	}
	public class Separador{
		string CadenaSeparadora;
		int Vez;
		public Separador(string cadenaSeparadora){
			this.CadenaSeparadora=cadenaSeparadora;
			this.Vez=0;
		}
		public static implicit operator string(Separador s){
			s.Vez++;
			return s.mismo();
		}
		public string mismo(){
			if(Vez==1){
				return "";
			}
			return CadenaSeparadora;
		}
		public static string Concatenar(ArrayList elementos,string separador){
			StringBuilder rta=new StringBuilder();
			Separador s=new Separador(separador);
			foreach(string elemento in elementos){
				rta.Append(s+elemento);
			}
			return rta.ToString();
		}
	}
	[TestFixture]
	public class ProbarSeparador{
		[Test]
		public void ProbarDirecto(){
			Separador coma=new Separador(",");
			Assert.AreEqual("uno",coma+"uno");
			Separador mas=new Separador("+");
			StringBuilder s=new StringBuilder();
			s.Append(mas+"uno");
			s.Append(mas+"dos");
			Assert.AreEqual("uno+dos",s.ToString());
		}
		[Test]
		public void ProbarConcatenar(){
			ArrayList datos=new ArrayList();
			Assert.AreEqual("",Separador.Concatenar(datos,","));
			datos.Add("uno");
			Assert.AreEqual("uno",Separador.Concatenar(datos,","));
			datos.Add("dos");
			Assert.AreEqual("uno,dos",Separador.Concatenar(datos,","));
			datos.Add("tres");
			Assert.AreEqual("uno; dos; tres",Separador.Concatenar(datos,"; "));
		}
		[Test]
		public void ProbarMismo(){
			string[] letras={"uno", "dos", "tres"};
			string[] numeros={"1","2","3"};
			string listaLetras="";
			string listaNumeros="";
			Separador mas=new Separador("+");
			for(int i=0; i<3; i++){
				listaLetras+=mas+letras[i];
				listaNumeros+=mas.mismo()+numeros[i];
			}
			Assert.AreEqual("uno+dos+tres",listaLetras);
			Assert.AreEqual("1+2+3",listaNumeros);
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

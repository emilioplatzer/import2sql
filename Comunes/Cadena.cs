/*
 * Creado por SharpDevelop.
 * Usuario: Administrador
 * Fecha: 16/02/2008
 * Hora: 12:19 p.m.
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificaci�n | Editar Encabezados Est�ndar
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
	public delegate string DelegateString_String(string que);
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
			return frase.Replace("�","a")
				.Replace("�","e")
				.Replace("�","i")
				.Replace("�","o")
				.Replace("�","u");
			/* Hoy aprendimos:
			 * Que no hay que hacer nada que dependa de la "localidad" de la maquina
			 * En una m�quina en ruso esto no funcionaba:
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
		public static string Simplificar(string valor){
			return Regex.Replace(Regex.Replace(
				valor.Replace('�','n')
				,"[\n\t\r\' ]+"," ")
				," ?(?<signo>[][,(){};.]) ?","${signo}");
			/*
			return valor.Replace('"',' ')
				.Replace('\'',' ')
				.Replace('\n',' ')
				.Replace('\r',' ')
				.Replace('\t',' ')
				.Replace('�','n')
				.Replace(
				.Substring(0,Otras.Min(250,valor.Length)).Trim();
				*/
		}
		public static string AgregarSiFalta(string frase, string ultimosCaracteres){
			if(frase.EndsWith(ultimosCaracteres)){
				return frase;
			}else{
				for(int i=1; i<ultimosCaracteres.Length; i++){
					if(frase.EndsWith(ultimosCaracteres.Remove(i))){
					   	return frase+ultimosCaracteres.Substring(ultimosCaracteres.Length-i);
				    }
				}
				return frase+ultimosCaracteres;
			}
		}
		/// <summary>
		/// Devuelve un string a�n cuando el par�metro es nulo
		/// </summary>
		public static string DelValor(object o){
			if(o==null){
				return "#null#";
			}else{
				return o.ToString();
			}
		}
	}

	[TestFixture]
	public class ProbarCadena{
		public ProbarCadena(){
		}
		[Test]
		public void CantidadOcurrencias(){
			Assert.AreEqual(0,Cadena.CantidadOcurrencias('i',"hola como and�s?"));
			Assert.AreEqual(3,Cadena.CantidadOcurrencias('o',"hola como and�s?"));
			Assert.IsFalse(Cadena.LlavesBalanceadas("{{{no}no{no"),"no");
			Assert.IsTrue(Cadena.LlavesBalanceadas("{{{no}no{no}}}si"),"si");
		}
		[Test]
		public void SacarAcentos(){
			Assert.AreEqual("hola como andas?",Cadena.SacarAcentos("hola como and�s?"));
		}
		[Test]
		public void SignoIgual(){
			Assert.AreEqual("hola che",Cadena.ExpandirSignoIgual("hola=20che"));
			Assert.AreEqual(@"La regi�n",Cadena.ExpandirSignoIgual("La regi=F3n"));
			Assert.AreEqual("Regi�n",Cadena.ExpandirSignoIgual("Regi=F3n"));
			Assert.AreEqual("L�nea",Cadena.ExpandirSignoIgual("L=EDnea"));
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
			pi+=1002000; // un mill�n dos mil
			Assert.AreEqual("1002003.14",pi.ToString(Cadena.FormatoPuntoDecimal));
		}
		/*
		[Test]
		public void Reemplazos(){
			Assert.AreEqual("hola",Cadena.BuscarYReemplazar("hola","e","a"));
			Assert.AreEqual("hala",Cadena.BuscarYReemplazar("hola","o","a"));
			Assert.AreEqual("taataa",Cadena.BuscarYReemplazar("tata","a","aa"));
			Assert.AreEqual("tatatatatata",Cadena.BuscarYReemplazar("tata","ta","tatata"));
			Assert.AreEqual("tet",Cadena.BuscarYReemplazar("ttt","tt","te"));
			Assert.AreEqual("tett",Cadena.BuscarYReemplazar("ttt","tt","tet"));
		}
		*/
		[Test]
		public void AgregarSiFalta(){
			Assert.AreEqual("-1 esto.",Cadena.AgregarSiFalta("-1 esto.","."));
			Assert.AreEqual("0 esto.",Cadena.AgregarSiFalta("0 esto","."));
			Assert.AreEqual("1 esto.-",Cadena.AgregarSiFalta("1 esto",".-"));
			Assert.AreEqual("2 esto.-",Cadena.AgregarSiFalta("2 esto.",".-"));
			Assert.AreEqual("3 esto.-",Cadena.AgregarSiFalta("3 esto.-",".-"));
			Assert.AreEqual("4 esto-.-",Cadena.AgregarSiFalta("4 esto-",".-"));
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
		string CadenaComenzadora;
		int Vez;
		int? anchoMaximo;
		int? LongitudAnterior;
		bool InvertirEspacioFinDeLinea;
		public Separador(string cadenaSeparadora)
			:this("",cadenaSeparadora)
		{} 
		public Separador(string cadenaComenzadora,string cadenaSeparadora){
			this.CadenaSeparadora=cadenaSeparadora;
			this.CadenaComenzadora=cadenaComenzadora;
			this.Vez=0;
		}
		public static implicit operator string(Separador s){
			s.Vez++;
			return s.mismo();
		}
		public string mismo(){
			if(Vez==1){
				return CadenaComenzadora;
			}
			return CadenaSeparadora;
		}
		public string Concatenar<T>(T[] elementos){
			StringBuilder rta=new StringBuilder();
			foreach(T elemento in elementos){
				//rta.Append(s+elemento);
				AgregarEn(rta,elemento.ToString());
			}
			return rta.ToString();
		}
		public static string Concatenar<T>(List<T> elementos,string separador){
			return Concatenar(elementos.ToArray(),separador);
		}
		public static string Concatenar<T>(T[] elementos,string separador){
			Separador s=new Separador(separador);
			return s.Concatenar(elementos);
		}
		public Separador AnchoMaximo(int cual){
			anchoMaximo=cual-CadenaSeparadora.Length*2;
			return this;
		}
		public Separador AnchoMaximo(){
			return AnchoMaximo(72);
		}
		public Separador AnchoLimitadoConIdentacion(){
			InvertirEspacioFinDeLinea=true;
			return AnchoMaximo();
		}
		public string ConFinDeLinea(){
			string rta=this;
			if(InvertirEspacioFinDeLinea && rta.EndsWith(" ")){
				return rta.Substring(0,rta.Length-1)+"\n ";
			}else{
				return rta+"\n";
			}
		}
		public void AgregarEn<T>(StringBuilder rta,T valor){
			if(anchoMaximo!=null){
				if(LongitudAnterior==null){
					int SaltoEnComenzador=CadenaComenzadora.LastIndexOf('\n');
					if(SaltoEnComenzador>=0){
						LongitudAnterior=rta.Length+SaltoEnComenzador;
					}else{
						LongitudAnterior=rta.ToString().LastIndexOf('\n');
						if(LongitudAnterior<0){
							
							LongitudAnterior=0;
						}
					}
				}
				int longitudAgregando=valor.ToString().Length;
				if(rta.Length+longitudAgregando-LongitudAnterior>anchoMaximo && rta.Length-LongitudAnterior>Otras.MIN<int>(CadenaComenzadora.Length+CadenaSeparadora.Length,anchoMaximo/10)){
					rta.Append(this.ConFinDeLinea());
					rta.Append(valor);
					LongitudAnterior=rta.Length;
				}else{
					rta.Append(this);
					rta.Append(valor);
				}
			}else{
				rta.Append(this);
				rta.Append(valor);
			}
		}
		public void Reiniciar(){
			Vez=0;
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
			List<string> datos=new List<string>();
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
		[Test]
		public void ProbarComenzador(){
			string[] letras={"uno", "dos", "tres"};
			string[] numeros={"1","2","3"};
			string lista="";
			Separador and=new Separador("WHERE "," AND ");
			for(int i=0; i<3; i++){
				lista+=and+letras[i]+"="+numeros[i];
			}
			Assert.AreEqual("WHERE uno=1 AND dos=2 AND tres=3",lista);
		}
		[Test]
		public void ProbarAnchoMaximo(){
			string[] numeros={"uno","dos","tres","cuatro","cinco","seis","siete","ocho","nueve","diez"};
			// Assert.Ignore("Tengo que arreglar esta pavada");
			{
				Separador coma=new Separador(", ").AnchoMaximo(13);
				Assert.AreEqual(
					"uno, dos, \ntres, cuatro, \ncinco, seis, \nsiete, ocho, \nnueve, diez"
					,coma.Concatenar(numeros)
				);
			}
			{
				StringBuilder s=new StringBuilder();
				s.Append("El primer renglon\n");
				Separador coma=new Separador(", ").AnchoMaximo(13);
				foreach(string n in numeros){
					coma.AgregarEn(s,n);
				}
				Assert.AreEqual(
					"El primer renglon\nuno, dos, \ntres, cuatro, \ncinco, seis, \nsiete, ocho, \nnueve, diez"
					,s.ToString()
				);
			}
			{
				StringBuilder s=new StringBuilder();
				s.Append("El primer renglon");
				Separador coma=new Separador(", ").AnchoMaximo(13);
				foreach(string n in numeros){
					coma.AgregarEn(s,n);
				}
				Assert.AreEqual(
					"El primer renglon\nuno, dos, tres, \ncuatro, cinco, \nseis, siete, \nocho, nueve, \ndiez"
					,s.ToString()
				);
			}
		}
	}
	public static class ParaCadena{
		public static string BlancosANull(this string esto){
			if(esto==null || esto.Trim()==""){
				return null;
			}
			return esto;
		}
	}
}

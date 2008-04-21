/*
 * Creado por SharpDevelop.
 * Usuario: Emilio
 * Fecha: 22/02/2008
 * Hora: 15:04
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Text.RegularExpressions;
using System.Reflection;
using System.ComponentModel;
using NUnit.Framework;

namespace Comunes
{
	/// <summary>
	/// Para contener los parámetros de la aplicación.
	/// Debería levantar las opciones de distintas fuentes:
	/// 	.ini
	/// 	.cfg
	/// 	del registro de windows
	/// 	de la línea de comandos
	/// 	de una ventana donde el usuario especifique lo que quiere
	/// Esta clase es genérica, de ella deberían heredar la
	/// 	de las aplicaciones
	/// </summary>
	public class Parametros
	{
		public enum Tipo {INI};
		public enum LeerPorDefecto {SI, NO};
		string NombreAplicacion;
		public Parametros(LeerPorDefecto queHacer,string NombreAplicacion)
		{
			this.NombreAplicacion=NombreAplicacion;
			if(queHacer==LeerPorDefecto.SI){
				LeerPorDefecto_();
			}
		}
		public Parametros(LeerPorDefecto queHacer)
			:this(queHacer,System.Windows.Forms.Application.ProductName)
		{}
		public void LeerString(string valores,Tipo tipo){
			string finDefinicion=";" ,medioDefinicion=":";
			if(tipo==Tipo.INI){
				finDefinicion="\r?\n";
				medioDefinicion="=";
			}
			string[] definiciones=Regex.Split(valores,finDefinicion);
			foreach(string definicion in definiciones){
				int posicion=definicion.IndexOf(medioDefinicion);
				if(posicion>=0){
					string variable=definicion.Substring(0,posicion);
					string valor=definicion.Substring(posicion+1);
					FieldInfo f=this.GetType().GetField(variable);
					if(f!= null){
						TypeConverter conv=TypeDescriptor.GetConverter(f.FieldType);
						if(conv.CanConvertFrom(typeof(string))){
							object objetoValor=conv.ConvertFrom(valor);
							f.SetValue(this,objetoValor);
						}
					}
				}
			}
		}
		void LeerPorDefecto_(){
			string ArchivoINI=this.NombreAplicacion+".ini";
			if(Archivo.Existe(ArchivoINI)){
				LeerString(Archivo.Leer(ArchivoINI), Tipo.INI);
			}
		}
	}
	public class ParametrosPrueba:Parametros{
		public string DirUno;
		public string Frase;
		public int Cantidad;
		public DateTime Fecha;
		public ParametrosPrueba(LeerPorDefecto queHacer):base(queHacer){}
	}
	[TestFixture]
	public class PruebasParametros{
		string VariablesPrueba1="DirUno=c:\\temp\nFrase=Los hermanos sean unidos\nCantidad=128\nFecha=20/12/2001";
		string VariablesPrueba2="DirUno=c:\\temp\\aux\nFecha=1/2/3\nFrase=No hay futuro\nCantidad=-1";
		[Test]
		public void DesdeString(){
			ParametrosPrueba p=new ParametrosPrueba(Parametros.LeerPorDefecto.NO);
			p.LeerString(VariablesPrueba1,Parametros.Tipo.INI);
			Assert.AreEqual("c:\\temp",p.DirUno);
			Assert.AreEqual("Los hermanos sean unidos",p.Frase);
			Assert.AreEqual(128,p.Cantidad);
			Assert.AreEqual(new DateTime(2001,12,20),p.Fecha);
		}
		public static void MostrarVariablesDelSistema(){
			Console.WriteLine("CommandLine:"+System.Environment.CommandLine);
			Console.WriteLine("CurrentDirectory:"+System.Environment.CurrentDirectory);
			Console.WriteLine("MachineName:"+System.Environment.MachineName);
			Console.WriteLine("OSVersion:"+System.Environment.OSVersion);
			Console.WriteLine("StackTrace:"+System.Environment.StackTrace);
			Console.WriteLine("UserDomainName:"+System.Environment.UserDomainName);
			Console.WriteLine("UserInteractive:"+System.Environment.UserInteractive);
			Console.WriteLine("UserName:"+System.Environment.UserName);
			Console.WriteLine("Version:"+System.Environment.Version);
			Console.WriteLine("WorkingSet:"+System.Environment.WorkingSet);
			Console.WriteLine("GetCommandLineArgs[0]:"+System.Environment.GetCommandLineArgs()[0]);
			Console.WriteLine("System.Windows.Forms.Application");
			Console.WriteLine("ProductName:"+System.Windows.Forms.Application.ProductName);
		}
		[Test]
		public void NombreAplicacion(){
			MostrarVariablesDelSistema();
			Assert.AreEqual("NUnit",System.Windows.Forms.Application.ProductName);
		}
		[Test]
		public void LeerPorDefecto(){
			Archivo.Escribir("NUnit.ini",VariablesPrueba2);
			ParametrosPrueba p=new ParametrosPrueba(Parametros.LeerPorDefecto.SI);
			Assert.AreEqual("c:\\temp\\aux",p.DirUno);
			Assert.AreEqual("No hay futuro",p.Frase);
			Assert.AreEqual(-1,p.Cantidad);
			Assert.AreEqual(new DateTime(2003,2,1),p.Fecha);
		}
	}
}

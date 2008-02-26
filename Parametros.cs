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

namespace TodoASql
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
		public Parametros()
		{
		}
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
	}
	public class ParametrosPrueba:Parametros{
		public string DirUno;
		public string Frase;
		public int Cantidad;
		public DateTime Fecha;
	}
	[TestFixture]
	public class PruebasParametros{
		string VariablesPrueba1="DirUno=c:\\temp\nFrase=Los hermanos sean unidos\nCantidad=128\nFecha=20/12/2001";
		[Test]
		public void DesdeString(){
			ParametrosPrueba p=new ParametrosPrueba();
			p.LeerString(VariablesPrueba1,Parametros.Tipo.INI);
			Assert.AreEqual("c:\\temp",p.DirUno);
			Assert.AreEqual("Los hermanos sean unidos",p.Frase);
			Assert.AreEqual(128,p.Cantidad);
			Assert.AreEqual(new DateTime(2001,12,20),p.Fecha);
		}
	}
}

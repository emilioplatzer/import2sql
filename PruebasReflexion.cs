/*
 * Creado por SharpDevelop.
 * Usuario: Emilio
 * Fecha: 22/02/2008
 * Hora: 15:09
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace TodoASql
{
	/// <summary>
	/// Description of PruebasReflexion.
	/// </summary>
	public class ClaseReflexiva
	{
		string campo1;
		int campo2;
		public ClaseReflexiva()
		{
			campo1="hola";
			campo2=3;
		}
		public string NombresMiembros(){
			StringBuilder rta=new StringBuilder();
			System.Reflection.MemberTypes mt=this.GetType().MemberType;
			System.Reflection.MemberInfo[] ms=this.GetType().GetMembers();
			foreach(MemberInfo m in ms){
				rta.Append(m.Name+",");
			}
			return rta.ToString();
		}
	}
	
	[TestFixture]
	class PruebasReflexion{
		public PruebasReflexion(){
		}
		[Test]
		public void NombresMiembros(){
			Assert.AreEqual("campo1,campo2"
			                ,new ClaseReflexiva().NombresMiembros());
		}
	}
}

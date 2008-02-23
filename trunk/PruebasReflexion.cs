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
		public string campo1;
		public int campo2;
		public ClaseReflexiva()
		{
			campo1="hola";
			campo2=3;
		}
		public override string ToString(){
			return campo1+campo2;
		}
		public string NombresMiembros(){
			StringBuilder rta=new StringBuilder();
			System.Reflection.MemberTypes mt=this.GetType().MemberType;
			System.Reflection.MemberInfo[] ms=this.GetType().GetMembers();
			Separador coma=new Separador(",");
			foreach(MemberInfo m in ms){
				rta.Append(coma+m.Name);
			}
			return rta.ToString();
		}
		public string NombresCampos(){
			StringBuilder rta=new StringBuilder();
			System.Reflection.FieldInfo[] ms=this.GetType().GetFields();
			Separador coma=new Separador(",");
			foreach(FieldInfo m in ms){
				rta.Append(coma+m.Name);
			}
			return rta.ToString();
		}
	}
	
	[TestFixture]
	public class PruebasReflexion{
		public PruebasReflexion(){
		}
		[Test]
		public void NombresMiembros(){
			ClaseReflexiva r=new ClaseReflexiva();
			Assert.AreEqual("campo1,campo2"
			                ,r.NombresCampos());
		}
	}
}

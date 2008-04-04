/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 04/04/2008
 * Time: 05:38 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Reflection;
// using System.Text;
using NUnit.Framework;
using CampoEntero=Modelador.CampoTipo<int>;
using Modelador;

namespace Modelador
{
	/// <summary>
	/// Description of Tabla.
	/// </summary>
	public abstract class Tabla
	{
		public Tabla()
		{
			Construir();
		}
		protected virtual void ConstruirCampos(){
      		Assembly assem = Assembly.GetExecutingAssembly();
      		System.Reflection.FieldInfo[] ms=this.GetType().GetFields(/*System.Reflection.BindingFlags.NonPublic*/);
			foreach(FieldInfo m in ms){
				if(m.FieldType.IsSubclassOf(typeof(Campo))){
      				Campo c=(Campo)assem.CreateInstance(m.FieldType.FullName);
      				/*
      				Campo c=assem.CreateInstance(m.FieldType.FullName,false,BindingFlags.ExactBinding, null,
      				                             new Object[] { m.Name }, null, null);
      				*/
      				c.Nombre=m.Name;
      				m.SetValue(this,c);
				}
			}
		}
		protected abstract void ConstruirPk();
		protected void ConstruirPk();
		protected virtual void ConstruirResto(){}
		protected void Construir(){
			ConstruirCampos();
			ConstruirPk();
			ConstruirResto();
		}
	}
	public class Campo
	{
		public string Nombre;
		public Campo(){
		}
		public void SeaPk(){}
	}
	public class CampoTipo<T>:Campo{
		public T valor;
		public CampoTipo()
		{	
			valor=default(T);
		}
		
	}
}
namespace PrModelador
{

	public class Periodo:Tabla{
		public CampoEntero cAno;
		public CampoEntero cMes;
		public CampoEntero cAnoAnt;
		public CampoEntero cMesAnt;
		protected override void ConstruirPk(){
			base.ConstruirPk(cAno,cMes);
		}
		protected override void ConstruirCampos()
		{
			base.ConstruirCampos();
			return;
		}
	}
	[TestFixture]
	public class prTabla{
		public prTabla(){
		}
		[Test]
		public void PrPeriodo(){
			Periodo p=new Periodo();
			Assert.AreEqual(0,p.cAno.valor);
			Assert.AreEqual("cAno",p.cAno.Nombre);
		}
	}
}

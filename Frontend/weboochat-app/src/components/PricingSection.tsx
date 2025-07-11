import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Check, Star, Zap, Crown } from "lucide-react";

const PricingSection = () => {
  const plans = [
    {
      name: "Basic",
      price: "$29",
      period: "/month",
      icon: Zap,
      description: "Perfect for small businesses",
      features: [
        "Up to 1,000 messages/month",
        "1 WhatsApp number",
        "Basic chatbot",
        "Email support",
        "Basic reports"
      ],
      recommended: false,
      color: "border-primary/20"
    },
    {
      name: "Professional",
      price: "$59",
      period: "/month",
      icon: Star,
      description: "For growing businesses",
      features: [
        "Up to 5,000 messages/month",
        "3 WhatsApp numbers",
        "Advanced ChatGPT",
        "Smart routing",
        "Priority support",
        "Advanced analytics",
        "CRM integration"
      ],
      recommended: true,
      color: "border-primary"
    },
    {
      name: "Enterprise",
      price: "$119",
      period: "/month",
      icon: Crown,
      description: "Complete corporate solution",
      features: [
        "Unlimited messages",
        "Unlimited numbers",
        "Custom ChatGPT",
        "Multi-agent support",
        "24/7 support",
        "Full API access",
        "White-label solution",
        "Team training"
      ],
      recommended: false,
      color: "border-secondary"
    }
  ];

  return (
    <section id="pricing" className="py-20 bg-background">
      <div className="container mx-auto px-4 lg:px-8">
        <div className="text-center mb-16">
          <h2 className="text-4xl md:text-5xl font-bold text-foreground mb-6">
            Choose your
            <span className="bg-gradient-primary bg-clip-text text-transparent"> Plan</span>
          </h2>
          <p className="text-xl text-muted-foreground max-w-3xl mx-auto">
            Flexible plans that grow with your business. No hidden fees, no bureaucracy.
          </p>
        </div>

        <div className="grid md:grid-cols-3 gap-8 max-w-6xl mx-auto">
          {plans.map((plan, index) => (
            <Card 
              key={index} 
              className={`relative group hover:shadow-card transition-all duration-300 hover:scale-105 bg-card/50 backdrop-blur-sm ${plan.color} ${
                plan.recommended ? 'ring-2 ring-primary shadow-glow' : ''
              }`}
            >
              {plan.recommended && (
                <div className="absolute -top-4 left-1/2 transform -translate-x-1/2">
                  <div className="bg-gradient-primary text-primary-foreground px-4 py-2 rounded-full text-sm font-semibold">
                    Most Popular
                  </div>
                </div>
              )}

              <CardHeader className="text-center pb-6">
                <div className="flex justify-center mb-4">
                  <div className={`p-3 rounded-lg bg-gradient-secondary ${
                    plan.recommended ? 'text-primary' : 'text-muted-foreground'
                  }`}>
                    <plan.icon size={32} />
                  </div>
                </div>
                <CardTitle className="text-2xl font-bold text-foreground">
                  {plan.name}
                </CardTitle>
                <p className="text-muted-foreground">{plan.description}</p>
                <div className="mt-4">
                  <span className="text-4xl font-bold text-foreground">{plan.price}</span>
                  <span className="text-muted-foreground">{plan.period}</span>
                </div>
              </CardHeader>

              <CardContent className="pt-0">
                <ul className="space-y-3 mb-8">
                  {plan.features.map((feature, featureIndex) => (
                    <li key={featureIndex} className="flex items-center space-x-3">
                      <Check className="w-5 h-5 text-primary flex-shrink-0" />
                      <span className="text-muted-foreground">{feature}</span>
                    </li>
                  ))}
                </ul>

                <Button 
                  variant={plan.recommended ? "hero" : "outline"} 
                  className="w-full"
                  size="lg"
                >
                  Get Started
                </Button>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Guarantee */}
        <div className="mt-16 text-center">
          <div className="inline-flex items-center space-x-2 bg-gradient-secondary rounded-full px-6 py-3 border border-primary/20">
            <Check className="w-5 h-5 text-primary" />
            <span className="text-sm font-medium text-foreground">
              7-day guarantee • Cancel anytime
            </span>
          </div>
        </div>
      </div>
    </section>
  );
};

export default PricingSection;